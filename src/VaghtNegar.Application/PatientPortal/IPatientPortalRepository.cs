namespace VaghtNegar.Application.PatientPortal;

public interface IPatientPortalRepository
{
    Task<PatientProfileDto?> GetProfileAsync(string identityUserId, CancellationToken cancellationToken);

    Task<PatientAppointmentsPageDto?> GetAppointmentsAsync(
        string identityUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PatientAppointmentDto?> CancelAppointmentAsync(
        string identityUserId,
        Guid appointmentId,
        CancellationToken cancellationToken);
}
