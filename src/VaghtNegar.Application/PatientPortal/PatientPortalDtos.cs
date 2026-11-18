namespace VaghtNegar.Application.PatientPortal;

public sealed record PatientProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string MobileNumber,
    DateOnly? BirthDate);

public sealed record PatientAppointmentDto(
    Guid Id,
    Guid ClinicId,
    Guid DoctorId,
    string DoctorFullName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    string? PatientNote,
    DateTimeOffset CreatedAtUtc);

public sealed record PatientAppointmentsPageDto(
    IReadOnlyList<PatientAppointmentDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
