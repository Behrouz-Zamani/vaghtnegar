using VaghtNegar.Application.Scheduling;
using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Application.Appointments;

public sealed class AppointmentBookingService(
    ISchedulingRepository schedulingRepository,
    IAppointmentBookingRepository bookingRepository,
    SchedulingService schedulingService)
{
    public async Task<AppointmentBookingDto> BookAsync(
        Guid doctorId,
        string identityUserId,
        BookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        ValidateBookingInput(doctorId, request.StartsAtUtc, request.PatientNote);
        if (string.IsNullOrWhiteSpace(identityUserId))
            throw new ArgumentException("شناسه کاربر احراز هویت‌شده معتبر نیست.");

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
            doctorId, identityUserId.Trim(), request.StartsAtUtc, endsAtUtc, request.PatientNote, cancellationToken);

        return MapBookingResult(result);
    }

    public async Task<AppointmentBookingDto> BookForPatientAsync(
        Guid doctorId,
        StaffBookAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        ValidateBookingInput(doctorId, request.StartsAtUtc, request.PatientNote);
        if (request.PatientId == Guid.Empty)
            throw new ArgumentException("انتخاب بیمار الزامی است.");

        var zone = GetTehranTimeZone();
        var localStart = TimeZoneInfo.ConvertTime(request.StartsAtUtc, zone);
        var localDate = DateOnly.FromDateTime(localStart.DateTime);
        var jalaliDate = SchedulingService.FormatJalaliDate(localDate);

        var availableSlots = await schedulingService.GetAvailableSlotsAsync(
            doctorId, jalaliDate, cancellationToken);

        var selectedSlot = availableSlots.FirstOrDefault(x => x.StartsAtUtc == request.StartsAtUtc);
        if (selectedSlot is null)
            throw new AppointmentSlotUnavailableException();

        var result = await bookingRepository.TryBookForPatientAsync(
            doctorId,
            request.PatientId,
            selectedSlot.StartsAtUtc,
            selectedSlot.EndsAtUtc,
            request.PatientNote,
            cancellationToken);

        return MapBookingResult(result);
    }

    private static void ValidateBookingInput(Guid doctorId, DateTimeOffset startsAtUtc, string? patientNote)
    {
        if (doctorId == Guid.Empty)
            throw new ArgumentException("شناسه پزشک معتبر نیست.");
        if (startsAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("زمان شروع باید با UTC و پسوند +00:00 ارسال شود.");
        if (startsAtUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentException("امکان رزرو زمان گذشته وجود ندارد.");
        if (patientNote?.Length > 1000)
            throw new ArgumentException("توضیحات بیمار حداکثر می‌تواند ۱۰۰۰ کاراکتر باشد.");
    }

    private static AppointmentBookingDto MapBookingResult(AppointmentBookingResult result)
    {
        if (result.Failure == AppointmentBookingFailure.DoctorNotFound)
            throw new DoctorForScheduleNotFoundException();
        if (result.Failure == AppointmentBookingFailure.PatientNotFound)
            throw new PatientForBookingNotFoundException();
        if (result.Failure == AppointmentBookingFailure.SlotUnavailable || result.Appointment is null)
            throw new AppointmentSlotUnavailableException();

        var appointment = result.Appointment;
        return new AppointmentBookingDto(
            appointment.Id, appointment.ClinicId, appointment.DoctorId, appointment.PatientId,
            appointment.StartsAtUtc, appointment.EndsAtUtc, appointment.Status.ToString(),
            appointment.PatientNote, appointment.CreatedAtUtc);
    }

    private static TimeZoneInfo GetTehranTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
    }
}

public sealed class PatientForBookingNotFoundException() : Exception("پرونده فعال بیمار یافت نشد.");
public sealed class AppointmentSlotUnavailableException() : Exception("این زمان قبلاً رزرو شده یا دیگر در دسترس نیست.");
