using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.Appointments;
using VaghtNegar.Application.Scheduling;
using VaghtNegar.Domain.Entities;
using VaghtNegar.Domain.Enums;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.Infrastructure.Appointments;

public sealed class AppointmentPaymentWorkflow(
    VaghtNegarDbContext db,
    SchedulingService scheduling) : IAppointmentPaymentWorkflow
{
    public async Task<BookingHoldCreatedDto> CreateHoldAsync(
        Guid doctorId, CreateBookingHoldRequest request, CancellationToken ct)
    {
        if (doctorId == Guid.Empty) throw new ArgumentException("پزشک معتبر نیست.");
        if (request.StartsAtUtc.Offset != TimeSpan.Zero || request.StartsAtUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentException("زمان انتخابی معتبر نیست و باید با UTC ارسال شود.");
        if (request.PatientNote?.Length > 1000) throw new ArgumentException("توضیحات حداکثر ۱۰۰۰ کاراکتر است.");

        var date = TimeZoneInfo.ConvertTime(request.StartsAtUtc, TehranZone()).DateTime;
        var slots = await scheduling.GetAvailableSlotsAsync(
            doctorId, SchedulingService.FormatJalaliDate(DateOnly.FromDateTime(date)), ct);
        var slot = slots.FirstOrDefault(x => x.StartsAtUtc == request.StartsAtUtc);
        if (slot is null) throw new InvalidOperationException("این ساعت دیگر آزاد نیست.");

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTimeOffset.UtcNow;
        var doctor = await db.Doctors.FirstOrDefaultAsync(x => x.Id == doctorId && x.IsActive, ct)
            ?? throw new KeyNotFoundException("پزشک فعال یافت نشد.");

        var expired = await db.AppointmentBookingRequests
            .Where(x => (x.Status == AppointmentBookingRequestStatus.AwaitingLogin ||
                         x.Status == AppointmentBookingRequestStatus.AwaitingPayment) &&
                        x.HoldExpiresAtUtc <= now)
            .ToListAsync(ct);
        foreach (var item in expired) item.ExpireIfNeeded(now);

        var appointmentConflict = await db.Appointments.AnyAsync(x =>
            x.DoctorId == doctorId && x.Status != AppointmentStatus.Cancelled &&
            x.StartsAtUtc < slot.EndsAtUtc && x.EndsAtUtc > slot.StartsAtUtc, ct);
        var requestConflict = await db.AppointmentBookingRequests.AnyAsync(x =>
            x.DoctorId == doctorId &&
            (x.Status == AppointmentBookingRequestStatus.AwaitingReview ||
             ((x.Status == AppointmentBookingRequestStatus.AwaitingLogin ||
               x.Status == AppointmentBookingRequestStatus.AwaitingPayment) && x.HoldExpiresAtUtc > now)) &&
            x.StartsAtUtc < slot.EndsAtUtc && x.EndsAtUtc > slot.StartsAtUtc, ct);

        if (appointmentConflict || requestConflict)
            throw new InvalidOperationException("این ساعت در همین لحظه توسط کاربر دیگری انتخاب شد.");

        var rawToken = CreateToken();
        var booking = new AppointmentBookingRequest(
            doctor.ClinicId, doctor.Id, slot.StartsAtUtc, slot.EndsAtUtc,
            doctor.VisitFeeToman, request.PatientNote, HashToken(rawToken));

        db.AppointmentBookingRequests.Add(booking);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new BookingHoldCreatedDto(
            booking.Id, rawToken, booking.HoldExpiresAtUtc, booking.VisitFeeToman,
            booking.StartsAtUtc, booking.EndsAtUtc, booking.Status.ToString());
    }

    public async Task<BookingRequestDto?> GetByTokenAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var booking = await db.AppointmentBookingRequests
            .FirstOrDefaultAsync(x => x.AccessTokenHash == HashToken(token), ct);
        if (booking is null) return null;
        if (booking.ExpireIfNeeded(DateTimeOffset.UtcNow)) await db.SaveChangesAsync(ct);
        return await MapAsync(booking.Id, ct);
    }

    public async Task<BookingRequestDto> ClaimAsync(string token, string identityUserId, CancellationToken ct)
    {
        var booking = await FindByTokenAsync(token, ct);
        var patient = await db.Patients.FirstOrDefaultAsync(
            x => x.IdentityUserId == identityUserId && x.IsActive, ct);
        if (patient is null)
            throw new InvalidOperationException("پرونده بیمار به حساب کاربری شما متصل نیست. ابتدا اتصال پرونده توسط پذیرش انجام شود.");
        booking.AttachPatient(patient.Id, identityUserId);
        await db.SaveChangesAsync(ct);
        return await MapAsync(booking.Id, ct);
    }

    public async Task<BookingRequestDto> SubmitReceiptAsync(
        string token, string identityUserId, SubmitPaymentReceiptRequest request, CancellationToken ct)
    {
        var booking = await FindByTokenAsync(token, ct);
        if (booking.IdentityUserId != identityUserId)
            throw new UnauthorizedAccessException("این درخواست متعلق به حساب کاربری شما نیست.");
        if (string.IsNullOrWhiteSpace(request.Base64Content))
            throw new ArgumentException("تصویر رسید الزامی است.");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(request.Base64Content); }
        catch (FormatException) { throw new ArgumentException("محتوای تصویر رسید معتبر نیست."); }
        if (bytes.Length > 5 * 1024 * 1024)
            throw new ArgumentException("حجم تصویر رسید نباید بیشتر از ۵ مگابایت باشد.");
        booking.SubmitReceipt(bytes, request.ContentType, request.FileName);
        await db.SaveChangesAsync(ct);
        return await MapAsync(booking.Id, ct);
    }

    public async Task<BookingRequestsPageDto> GetMyRequestsAsync(
        string identityUserId, int page, int pageSize, CancellationToken ct)
    {
        ValidatePaging(ref page, ref pageSize);
        var patientId = await db.Patients.Where(x => x.IdentityUserId == identityUserId && x.IsActive)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!patientId.HasValue) return new BookingRequestsPageDto([], page, pageSize, 0, 0);
        return await GetPageAsync(
            db.AppointmentBookingRequests.Where(x => x.PatientId == patientId.Value),
            page, pageSize, ct);
    }

    public Task<BookingRequestsPageDto> GetPendingReviewAsync(int page, int pageSize, CancellationToken ct)
    {
        ValidatePaging(ref page, ref pageSize);
        return GetPageAsync(
            db.AppointmentBookingRequests.Where(x => x.Status == AppointmentBookingRequestStatus.AwaitingReview),
            page, pageSize, ct);
    }

    public async Task<BookingRequestDto> ApproveAsync(
        Guid requestId, string reviewerIdentityUserId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var booking = await db.AppointmentBookingRequests.FirstOrDefaultAsync(x => x.Id == requestId, ct)
            ?? throw new KeyNotFoundException("درخواست نوبت یافت نشد.");
        if (booking.PatientId is null) throw new InvalidOperationException("پرونده بیمار به درخواست متصل نشده است.");

        var conflict = await db.Appointments.AnyAsync(x =>
            x.DoctorId == booking.DoctorId && x.Status != AppointmentStatus.Cancelled &&
            x.StartsAtUtc < booking.EndsAtUtc && x.EndsAtUtc > booking.StartsAtUtc, ct);
        if (conflict) throw new InvalidOperationException("این ساعت قبلاً به نوبت دیگری اختصاص یافته است.");

        booking.Approve(reviewerIdentityUserId);
        var appointment = new Appointment(
            booking.ClinicId, booking.DoctorId, booking.PatientId.Value,
            booking.StartsAtUtc, booking.EndsAtUtc, booking.PatientNote);
        appointment.Confirm();
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return await MapAsync(requestId, ct);
    }

    public async Task<BookingRequestDto> RejectAsync(
        Guid requestId, string reviewerIdentityUserId, string reason, CancellationToken ct)
    {
        var booking = await db.AppointmentBookingRequests.FirstOrDefaultAsync(x => x.Id == requestId, ct)
            ?? throw new KeyNotFoundException("درخواست نوبت یافت نشد.");
        booking.Reject(reviewerIdentityUserId, reason);
        await db.SaveChangesAsync(ct);
        return await MapAsync(requestId, ct);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)?> GetReceiptAsync(
        Guid requestId, CancellationToken ct)
    {
        var item = await db.AppointmentBookingRequests.AsNoTracking()
            .Where(x => x.Id == requestId && x.ReceiptContent != null)
            .Select(x => new { x.ReceiptContent, x.ReceiptContentType, x.ReceiptFileName })
            .FirstOrDefaultAsync(ct);
        return item?.ReceiptContent is null ? null :
            (item.ReceiptContent, item.ReceiptContentType ?? "application/octet-stream",
                item.ReceiptFileName ?? "receipt");
    }

    private async Task<AppointmentBookingRequest> FindByTokenAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("شناسه پیگیری معتبر نیست.");
        var booking = await db.AppointmentBookingRequests
            .FirstOrDefaultAsync(x => x.AccessTokenHash == HashToken(token), ct)
            ?? throw new KeyNotFoundException("درخواست نوبت یافت نشد یا شناسه پیگیری نامعتبر است.");
        if (booking.ExpireIfNeeded(DateTimeOffset.UtcNow))
        {
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("مهلت ۱۰ دقیقه‌ای رزرو به پایان رسیده و ساعت آزاد شده است.");
        }
        return booking;
    }

    private async Task<BookingRequestDto> MapAsync(Guid id, CancellationToken ct) =>
        await (from booking in db.AppointmentBookingRequests.AsNoTracking()
               join doctor in db.Doctors.AsNoTracking() on booking.DoctorId equals doctor.Id
               where booking.Id == id
               select new BookingRequestDto(
                   booking.Id, booking.DoctorId, doctor.FirstName + " " + doctor.LastName,
                   booking.StartsAtUtc, booking.EndsAtUtc, booking.VisitFeeToman,
                   booking.Status.ToString(), booking.HoldExpiresAtUtc, booking.CreatedAtUtc,
                   booking.ReceiptSubmittedAtUtc, booking.AdminNote, booking.ReceiptContent != null))
            .FirstAsync(ct);

    private async Task<BookingRequestsPageDto> GetPageAsync(
        IQueryable<AppointmentBookingRequest> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var ids = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => x.Id).ToListAsync(ct);
        var items = new List<BookingRequestDto>(ids.Count);
        foreach (var id in ids) items.Add(await MapAsync(id, ct));
        return new BookingRequestsPageDto(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize));
    }

    private static void ValidatePaging(ref int page, ref int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
    }

    private static string CreateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static TimeZoneInfo TehranZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
    }
}
