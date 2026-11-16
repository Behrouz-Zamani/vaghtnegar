using VaghtNegar.Application.Scheduling;
using VaghtNegar.Domain.Enums;

namespace VaghtNegar.Application.Appointments;

public sealed class AppointmentManagementService(IAppointmentManagementRepository repository)
{
    public async Task<PagedAppointmentsDto> GetListAsync(
        Guid? doctorId,
        Guid? patientId,
        string? status,
        string? fromDate,
        string? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (doctorId == Guid.Empty || patientId == Guid.Empty)
            throw new ArgumentException("شناسه پزشک یا بیمار معتبر نیست.");
        if (page < 1) throw new ArgumentException("شماره صفحه باید حداقل ۱ باشد.");
        if (pageSize is < 1 or > 100) throw new ArgumentException("اندازه صفحه باید بین ۱ تا ۱۰۰ باشد.");

        AppointmentStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<AppointmentStatus>(status, true, out var value) ||
                !Enum.IsDefined(value))
                throw new ArgumentException("وضعیت نوبت معتبر نیست. مقادیر مجاز: Pending, Confirmed, Cancelled, Completed, NoShow.");
            parsedStatus = value;
        }

        var zone = GetTehranTimeZone();
        DateTimeOffset? fromUtc = null;
        DateTimeOffset? toUtc = null;

        if (!string.IsNullOrWhiteSpace(fromDate))
        {
            var date = SchedulingService.ParseJalaliDate(fromDate);
            var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            fromUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
        }

        if (!string.IsNullOrWhiteSpace(toDate))
        {
            var date = SchedulingService.ParseJalaliDate(toDate).AddDays(1);
            var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            toUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
        }

        if (fromUtc.HasValue && toUtc.HasValue && fromUtc >= toUtc)
            throw new ArgumentException("بازه تاریخ نامعتبر است؛ تاریخ پایان باید مساوی یا بعد از تاریخ شروع باشد.");

        return await repository.GetListAsync(
            doctorId, patientId, parsedStatus, fromUtc, toUtc, page, pageSize, cancellationToken);
    }

    public Task<AppointmentManagementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw new ArgumentException("شناسه نوبت معتبر نیست.");
        return repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<AppointmentManagementDto> ChangeStatusAsync(
        Guid id,
        string action,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty) throw new ArgumentException("شناسه نوبت معتبر نیست.");

        var targetStatus = action.Trim().ToLowerInvariant() switch
        {
            "confirm" => AppointmentStatus.Confirmed,
            "cancel" => AppointmentStatus.Cancelled,
            "complete" => AppointmentStatus.Completed,
            "no-show" => AppointmentStatus.NoShow,
            _ => throw new ArgumentException("عملیات وضعیت نامعتبر است.")
        };

        try
        {
            var result = await repository.ChangeStatusAsync(id, targetStatus, cancellationToken);
            return result ?? throw new AppointmentForManagementNotFoundException();
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidAppointmentStatusTransitionException(ex.Message);
        }
    }

    private static TimeZoneInfo GetTehranTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
    }
}

public sealed class AppointmentForManagementNotFoundException() : Exception("نوبت موردنظر یافت نشد.");
public sealed class InvalidAppointmentStatusTransitionException(string message) : Exception(message);
