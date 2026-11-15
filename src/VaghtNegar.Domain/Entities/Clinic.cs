namespace VaghtNegar.Domain.Entities;

public sealed class Clinic
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private Clinic() { }

    public Clinic(string name, string slug)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Clinic name is required.", nameof(name)) : name.Trim();
        Slug = string.IsNullOrWhiteSpace(slug) ? throw new ArgumentException("Clinic slug is required.", nameof(slug)) : slug.Trim().ToLowerInvariant();
    }
}
