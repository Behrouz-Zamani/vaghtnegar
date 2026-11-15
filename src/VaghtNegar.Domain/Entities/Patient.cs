namespace VaghtNegar.Domain.Entities;

public sealed class Patient
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string MobileNumber { get; private set; } = string.Empty;
    public DateOnly? BirthDate { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private Patient() { }

    public Patient(string firstName, string lastName, string mobileNumber, DateOnly? birthDate = null)
    {
        FirstName = string.IsNullOrWhiteSpace(firstName) ? throw new ArgumentException("First name is required.", nameof(firstName)) : firstName.Trim();
        LastName = string.IsNullOrWhiteSpace(lastName) ? throw new ArgumentException("Last name is required.", nameof(lastName)) : lastName.Trim();
        MobileNumber = string.IsNullOrWhiteSpace(mobileNumber) ? throw new ArgumentException("Mobile number is required.", nameof(mobileNumber)) : mobileNumber.Trim();
        BirthDate = birthDate;
    }
}
