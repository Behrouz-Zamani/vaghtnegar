namespace VaghtNegar.Domain.Entities;

public sealed class Doctor
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ClinicId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? MedicalCode { get; private set; }
    public long VisitFeeToman { get; private set; }
    public string? IdentityUserId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Doctor() { }

    public Doctor(Guid clinicId, string firstName, string lastName, string? medicalCode, long visitFeeToman)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("Clinic is required.", nameof(clinicId));
        ClinicId = clinicId;
        SetDetails(firstName, lastName, medicalCode);
        SetVisitFee(visitFeeToman);
    }

    public void UpdateDetails(string firstName, string lastName, string? medicalCode, long visitFeeToman)
    {
        SetDetails(firstName, lastName, medicalCode);
        SetVisitFee(visitFeeToman);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void SetVisitFee(long visitFeeToman)
    {
        if (visitFeeToman < 0) throw new ArgumentOutOfRangeException(nameof(visitFeeToman), "Visit fee cannot be negative.");
        VisitFeeToman = visitFeeToman;
    }

    public void LinkIdentity(string? identityUserId)
    {
        IdentityUserId = string.IsNullOrWhiteSpace(identityUserId) ? null : identityUserId.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void SetDetails(string firstName, string lastName, string? medicalCode)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));
        if (firstName.Trim().Length > 100) throw new ArgumentException("First name cannot exceed 100 characters.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.", nameof(lastName));
        if (lastName.Trim().Length > 100) throw new ArgumentException("Last name cannot exceed 100 characters.", nameof(lastName));
        if (!string.IsNullOrWhiteSpace(medicalCode) && medicalCode.Trim().Length > 50)
            throw new ArgumentException("Medical code cannot exceed 50 characters.", nameof(medicalCode));
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MedicalCode = string.IsNullOrWhiteSpace(medicalCode) ? null : medicalCode.Trim();
    }
}