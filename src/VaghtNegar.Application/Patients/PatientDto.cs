namespace VaghtNegar.Application.Patients;

public sealed record PatientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string MobileNumber,
    DateOnly? BirthDate,
    bool IsActive,
    bool HasLinkedIdentity,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
