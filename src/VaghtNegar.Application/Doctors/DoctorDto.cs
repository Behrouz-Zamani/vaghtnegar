namespace VaghtNegar.Application.Doctors;

public sealed record DoctorDto(Guid Id, Guid ClinicId, string FirstName, string LastName, string? MedicalCode, long VisitFeeToman, bool HasLinkedIdentity, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);