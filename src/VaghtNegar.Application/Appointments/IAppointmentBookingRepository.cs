namespace VaghtNegar.Application.Appointments;

public interface IAppointmentBookingRepository
{
    Task<AppointmentBookingResult> TryBookAsync(
        Guid doctorId,
        string identityUserId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        string? patientNote,
        CancellationToken cancellationToken);
}
