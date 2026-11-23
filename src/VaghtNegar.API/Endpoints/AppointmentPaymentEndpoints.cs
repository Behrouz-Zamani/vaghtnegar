using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.Appointments;
using VaghtNegar.Domain.Entities;
using VaghtNegar.Domain.Enums;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.API.Endpoints;

public static class AppointmentPaymentEndpoints
{
    private static readonly string[] StaffRoles = ["Receptionist", "ClinicAdmin", "SuperAdmin"];

    public static IEndpointRouteBuilder MapAppointmentPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var patient = endpoints.MapGroup("/api/v1/me/appointment-payments")
            .WithTags("Appointment Payments")
            .RequireAuthorization(policy => policy.RequireRole("Patient"));

        patient.MapPost("/checkout", async Task<IResult> (
            CheckoutRequest request, ClaimsPrincipal user, AppointmentBookingService booking,
            VaghtNegarDbContext db, IConfiguration configuration, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            if (request.StartsAtUtc <= DateTimeOffset.UtcNow.AddMinutes(10))
                return Results.BadRequest(new { message = "برای نگه‌داشتن زمان، نوبت باید حداقل ۱۰ دقیقه بعد از زمان فعلی باشد." });
            try
            {
                var appointment = await booking.BookAsync(request.DoctorId, subject,
                    new BookAppointmentRequest(request.StartsAtUtc, request.PatientNote), ct);
                var doctor = await db.Doctors.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.DoctorId, ct);
                if (doctor is null) return Results.NotFound(new { message = "پزشک یافت نشد." });

                var payment = new AppointmentPayment(appointment.Id, appointment.PatientId, appointment.DoctorId,
                    doctor.VisitFeeToman, DateTimeOffset.UtcNow.AddMinutes(10));
                db.AppointmentPayments.Add(payment);
                await db.SaveChangesAsync(ct);

                return Results.Created($"/api/v1/me/appointment-payments/{payment.Id}", new
                {
                    payment.Id, payment.AppointmentId, payment.AmountToman, status = payment.Status.ToString(),
                    payment.ExpiresAtUtc,
                    bankName = configuration["Payments:BankName"] ?? "",
                    accountHolder = configuration["Payments:AccountHolder"] ?? "",
                    cardNumber = configuration["Payments:CardNumber"] ?? "",
                    iban = configuration["Payments:Iban"] ?? ""
                });
            }
            catch (DoctorForScheduleNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (PatientForBookingNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (AppointmentSlotUnavailableException ex) { return Results.Conflict(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        })
        .WithName("CreateAppointmentCheckout")
        .WithSummary("ایجاد درخواست نوبت و نگه‌داشتن زمان به مدت ۱۰ دقیقه برای پرداخت");

        patient.MapGet("", async (ClaimsPrincipal user, VaghtNegarDbContext db, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            var patientId = await db.Patients.Where(x => x.IdentityUserId == subject && x.IsActive)
                .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
            if (patientId is null) return Results.NotFound(new { message = "پرونده بیمار متصل نیست." });
            await ExpireDueAsync(db, ct);
            var rows = await db.AppointmentPayments.AsNoTracking().Where(x => x.PatientId == patientId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new { x.Id, x.AppointmentId, x.DoctorId, doctorName = db.Doctors.Where(d => d.Id == x.DoctorId).Select(d => d.FirstName + " " + d.LastName).FirstOrDefault(),
                    x.AmountToman, status = x.Status.ToString(), x.CreatedAtUtc, x.ExpiresAtUtc,
                    x.ReceiptReference, x.ReceiptSubmittedAtUtc, x.ReviewNote,
                    x.Appointment.StartsAtUtc, x.Appointment.EndsAtUtc })
                .ToListAsync(ct);
            return Results.Ok(rows);
        })
        .WithName("GetMyAppointmentPayments");

        patient.MapPost("/{id:guid}/receipt", async Task<IResult> (
            Guid id, ReceiptRequest request, ClaimsPrincipal user, VaghtNegarDbContext db, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            var patientId = await db.Patients.Where(p => p.IdentityUserId == subject && p.IsActive)
                .Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
            if (patientId is null) return Results.NotFound(new { message = "پرونده بیمار یافت نشد." });
            var payment = await db.AppointmentPayments.Include(x => x.Appointment)
                .FirstOrDefaultAsync(x => x.Id == id && x.PatientId == patientId, ct);
            if (payment is null) return Results.NotFound(new { message = "درخواست پرداخت یافت نشد." });
            if (payment.ExpireIfDue(DateTimeOffset.UtcNow))
            {
                if (payment.Appointment.Status == AppointmentStatus.Pending) payment.Appointment.ReleasePaymentHold();
                await db.SaveChangesAsync(ct);
                return Results.Conflict(new { message = "مهلت پرداخت تمام شده است؛ زمان نوبت آزاد شد." });
            }
            try
            {
                payment.SubmitReceipt(request.Reference, request.ContentType, request.ImageBase64);
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { payment.Id, status = payment.Status.ToString(), message = "رسید برای بررسی ارسال شد." });
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
        })
        .WithName("SubmitAppointmentPaymentReceipt");

        var staff = endpoints.MapGroup("/api/v1/appointment-payments")
            .WithTags("Appointment Payments")
            .RequireAuthorization(policy => policy.RequireRole(StaffRoles));

        staff.MapGet("/pending", async (VaghtNegarDbContext db, CancellationToken ct) =>
        {
            await ExpireDueAsync(db, ct);
            var rows = await (from payment in db.AppointmentPayments.AsNoTracking()
                join appointment in db.Appointments on payment.AppointmentId equals appointment.Id
                join patientRecord in db.Patients on payment.PatientId equals patientRecord.Id
                join doctorRecord in db.Doctors on payment.DoctorId equals doctorRecord.Id
                where payment.Status == AppointmentPaymentStatus.ReceiptSubmitted
                orderby payment.ReceiptSubmittedAtUtc
                select new
                {
                    payment.Id, payment.AppointmentId, payment.PatientId, payment.DoctorId, payment.AmountToman,
                    status = payment.Status.ToString(), payment.ReceiptReference, payment.ReceiptContentType,
                    payment.ReceiptImageBase64, payment.ReceiptSubmittedAtUtc,
                    patientName = patientRecord.FirstName + " " + patientRecord.LastName,
                    doctorName = doctorRecord.FirstName + " " + doctorRecord.LastName,
                    appointment.StartsAtUtc, appointment.EndsAtUtc
                }).ToListAsync(ct);
            return Results.Ok(rows);
        }).WithName("GetPendingAppointmentPayments");

        staff.MapPost("/{id:guid}/approve", async Task<IResult> (
            Guid id, ClaimsPrincipal user, VaghtNegarDbContext db, CancellationToken ct) =>
        {
            var payment = await db.AppointmentPayments.Include(x => x.Appointment)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (payment is null) return Results.NotFound(new { message = "پرداخت یافت نشد." });
            if (payment.ExpireIfDue(DateTimeOffset.UtcNow))
            {
                if (payment.Appointment.Status == AppointmentStatus.Pending) payment.Appointment.Cancel();
                await db.SaveChangesAsync(ct);
                return Results.Conflict(new { message = "مهلت پرداخت تمام شده و نوبت آزاد شده است." });
            }
            try
            {
                payment.Approve(user.FindFirst("preferred_username")?.Value ?? user.FindFirst("sub")?.Value ?? "staff");
                payment.Appointment.Confirm();
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { payment.Id, status = payment.Status.ToString(), appointmentStatus = payment.Appointment.Status.ToString() });
            }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
        }).WithName("ApproveAppointmentPayment");

        staff.MapPost("/{id:guid}/reject", async Task<IResult> (
            Guid id, RejectReceiptRequest request, ClaimsPrincipal user, VaghtNegarDbContext db, CancellationToken ct) =>
        {
            var payment = await db.AppointmentPayments.Include(x => x.Appointment)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (payment is null) return Results.NotFound(new { message = "پرداخت یافت نشد." });
            if (payment.ExpireIfDue(DateTimeOffset.UtcNow))
            {
                if (payment.Appointment.Status == AppointmentStatus.Pending) payment.Appointment.Cancel();
                await db.SaveChangesAsync(ct);
                return Results.Conflict(new { message = "مهلت پرداخت تمام شده و نوبت آزاد شده است." });
            }
            try
            {
                payment.Reject(user.FindFirst("preferred_username")?.Value ?? user.FindFirst("sub")?.Value ?? "staff", request.Reason);
                if (payment.Appointment.Status == AppointmentStatus.Pending) payment.Appointment.Cancel();
                await db.SaveChangesAsync(ct);
                return Results.Ok(new { payment.Id, status = payment.Status.ToString(), payment.ReviewNote });
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
        }).WithName("RejectAppointmentPayment");

        return endpoints;
    }

    public static async Task ExpireDueAsync(VaghtNegarDbContext db, CancellationToken ct)
    {
        var due = await db.AppointmentPayments.Include(x => x.Appointment)
            .Where(x => (x.Status == AppointmentPaymentStatus.AwaitingReceipt ||
                         x.Status == AppointmentPaymentStatus.ReceiptSubmitted) &&
                        x.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            .ToListAsync(ct);
        foreach (var payment in due)
        {
            if (payment.ExpireIfDue(DateTimeOffset.UtcNow) &&
                payment.Appointment.Status == AppointmentStatus.Pending)
                payment.Appointment.Cancel();
        }
        if (due.Count > 0) await db.SaveChangesAsync(ct);
    }

    public sealed record CheckoutRequest(Guid DoctorId, DateTimeOffset StartsAtUtc, string? PatientNote);
    public sealed record ReceiptRequest(string Reference, string ContentType, string ImageBase64);
    public sealed record RejectReceiptRequest(string Reason);
}
