namespace VaghtNegar.Application.Appointments;

public sealed record AppointmentManagementDto(
    Guid Id,
    Guid ClinicId,
    Guid DoctorId,
    string DoctorFullName,
    Guid PatientId,
    string PatientFullName,
    string PatientMobileNumber,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    string? PatientNote,
    DateTimeOffset CreatedAtUtc);

public sealed record PagedAppointmentsDto(
    IReadOnlyList<AppointmentManagementDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
