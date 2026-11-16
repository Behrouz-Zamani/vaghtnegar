using System.Globalization;
using VaghtNegar.Application.Scheduling;
using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Application.Appointments;

public sealed class AppointmentBookingService(
    ISchedulingRepository schedulingRepository,
    IAppointmentBookingRepository bookingRepository)
{
    public async Task<AppointmentBookingDto> BookAsync(
        Guid doctorId,
        BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        if (doctorId == Guid.Empty)
            throw new ArgumentException("شناسه پزشک معتبر نیست.");
        if (request.PatientId == Guid.Empty)
            throw new ArgumentException("شناسه بیمار الزامی است.");
        if (request.StartsAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("زمان شروع باید با UTC و پسوند +00:00 ارسال شود.");
        if (request.StartsAtUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentException("امکان رزرو زمان گذشته وجود ندارد.");
        if (request.PatientNote?.Length > 1000)
            throw new ArgumentException("توضیحات بیمار حداکثر می‌تواند ۱۰۰۰ کاراکتر باشد.");

        if (!await schedulingRepository.DoctorExistsAsync(doctorId, cancellationToken))
            throw new DoctorForScheduleNotFoundException();

        var zone = GetTehranTimeZone();
        var localStart = TimeZoneInfo.ConvertTime(request.StartsAtUtc, zone);
        var localDate = DateOnly.FromDateTime(localStart.DateTime);
        var localTime = TimeOnly.FromDateTime(localStart.DateTime);

        var workingHours = await schedulingRepository.GetWorkingHoursAsync(doctorId, cancellationToken);
        var matchingRange = workingHours.FirstOrDefault(x =>
            x.IsActive &&
            x.DayOfWeek == localDate.DayOfWeek &&
            localTime >= x.StartTime &&
            localTime < x.EndTime &&
            (localTime.Ticks - x.StartTime.Ticks) % TimeSpan.FromMinutes(x.SlotDurationMinutes).Ticks == 0);

        if (matchingRange is null)
            throw new ArgumentException("زمان انتخاب‌شده در برنامه کاری پزشک نیست یا شروع یک نوبت معتبر محسوب نمی‌شود.");

        var localEnd = localTime.AddMinutes(matchingRange.SlotDurationMinutes);
        if (localEnd > matchingRange.EndTime)
            throw new ArgumentException("مدت نوبت از پایان بازه کاری پزشک عبور می‌کند.");

        var endLocalDateTime = localDate.ToDateTime(localEnd, DateTimeKind.Unspecified);
        var endsAtUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endLocalDateTime, zone), TimeSpan.Zero);

        var result = await bookingRepository.TryBookAsync(
            doctorId,
            request.PatientId,
            request.StartsAtUtc,
            endsAtUtc,
            request.PatientNote,
            cancellationToken);

        if (result.Failure == AppointmentBookingFailure.DoctorNotFound)
            throw new DoctorForScheduleNotFoundException();
        if (result.Failure == AppointmentBookingFailure.PatientNotFound)
            throw new PatientForBookingNotFoundException();
        if (result.Failure == AppointmentBookingFailure.SlotUnavailable || result.Appointment is null)
            throw new AppointmentSlotUnavailableException();

        return ToDto(result.Appointment);
    }

    private static AppointmentBookingDto ToDto(Appointment x) =>
        new(x.Id, x.ClinicId, x.DoctorId, x.PatientId, x.StartsAtUtc, x.EndsAtUtc,
            x.Status.ToString(), x.PatientNote, x.CreatedAtUtc);

    private static TimeZoneInfo GetTehranTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
    }
}

public sealed class PatientForBookingNotFoundException() : Exception("بیمار فعال موردنظر یافت نشد.");
public sealed class AppointmentSlotUnavailableException() : Exception("این زمان قبلاً رزرو شده یا دیگر در دسترس نیست.");
