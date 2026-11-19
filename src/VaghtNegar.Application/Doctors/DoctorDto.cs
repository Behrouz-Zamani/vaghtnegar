namespace VaghtNegar.Application.Doctors;

public sealed record DoctorDto(Guid Id, Guid ClinicId, string FirstName, string LastName, string? MedicalCode, bool HasLinkedIdentity, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);