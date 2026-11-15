namespace VaghtNegar.Domain.Entities;

public sealed class Patient
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string MobileNumber { get; private set; } = string.Empty;
    public DateOnly? BirthDate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Patient() { }

    public Patient(string firstName, string lastName, string mobileNumber, DateOnly? birthDate = null)
    {
        SetDetails(firstName, lastName, mobileNumber, birthDate);
    }

    public void UpdateDetails(string firstName, string lastName, string mobileNumber, DateOnly? birthDate)
    {
        SetDetails(firstName, lastName, mobileNumber, birthDate);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void SetDetails(string firstName, string lastName, string mobileNumber, DateOnly? birthDate)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("First name is required.", nameof(firstName));
        if (firstName.Trim().Length > 100) throw new ArgumentException("First name cannot exceed 100 characters.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("Last name is required.", nameof(lastName));
        if (lastName.Trim().Length > 100) throw new ArgumentException("Last name cannot exceed 100 characters.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(mobileNumber)) throw new ArgumentException("Mobile number is required.", nameof(mobileNumber));

        var normalizedMobile = new string(mobileNumber.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (normalizedMobile.Length > 20) throw new ArgumentException("Mobile number cannot exceed 20 characters.", nameof(mobileNumber));
        if (birthDate.HasValue && birthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Birth date cannot be in the future.", nameof(birthDate));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MobileNumber = normalizedMobile;
        BirthDate = birthDate;
    }
}
