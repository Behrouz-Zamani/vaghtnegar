namespace VaghtNegar.Domain.Entities;

public sealed class Doctor
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ClinicId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? MedicalCode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private Doctor() { }

    public Doctor(Guid clinicId, string firstName, string lastName, string? medicalCode = null)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("Clinic is required.", nameof(clinicId));
        ClinicId = clinicId;
        FirstName = string.IsNullOrWhiteSpace(firstName) ? throw new ArgumentException("First name is required.", nameof(firstName)) : firstName.Trim();
        LastName = string.IsNullOrWhiteSpace(lastName) ? throw new ArgumentException("Last name is required.", nameof(lastName)) : lastName.Trim();
        MedicalCode = string.IsNullOrWhiteSpace(medicalCode) ? null : medicalCode.Trim();
    }
}
