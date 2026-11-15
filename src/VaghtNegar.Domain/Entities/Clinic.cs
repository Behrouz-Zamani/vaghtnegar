namespace VaghtNegar.Domain.Entities;

public sealed class Clinic
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Clinic() { }

    public Clinic(string name, string slug)
    {
        SetDetails(name, slug);
    }

    public void UpdateDetails(string name, string slug)
    {
        SetDetails(name, slug);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void SetDetails(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Clinic name is required.", nameof(name));
        if (name.Trim().Length > 200)
            throw new ArgumentException("Clinic name cannot exceed 200 characters.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Clinic slug is required.", nameof(slug));
        if (slug.Trim().Length > 120)
            throw new ArgumentException("Clinic slug cannot exceed 120 characters.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
    }
}
