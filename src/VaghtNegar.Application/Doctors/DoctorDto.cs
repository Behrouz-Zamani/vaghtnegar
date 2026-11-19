namespace VaghtNegar.Application.Doctors;

public sealed record DoctorDto(Guid Id, Guid ClinicId, string FirstName, string LastName, string? MedicalCode, string? IdentityUserId, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);