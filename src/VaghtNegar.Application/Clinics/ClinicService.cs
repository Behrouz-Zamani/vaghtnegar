using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Application.Clinics;

public sealed class ClinicService(IClinicRepository repository)
{
    public async Task<IReadOnlyList<ClinicDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var clinics = await repository.GetAllAsync(cancellationToken);
        return clinics.Select(ToDto).ToArray();
    }

    public async Task<ClinicDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var clinic = await repository.GetByIdAsync(id, cancellationToken);
        return clinic is null ? null : ToDto(clinic);
    }

    public async Task<ClinicDto> CreateAsync(string name, string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = NormalizeSlug(slug);
        if (await repository.SlugExistsAsync(normalizedSlug, null, cancellationToken))
            throw new ClinicSlugAlreadyExistsException(normalizedSlug);

        var clinic = new Clinic(name, normalizedSlug);
        await repository.AddAsync(clinic, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(clinic);
    }

    public async Task<ClinicDto?> UpdateAsync(Guid id, string name, string slug, CancellationToken cancellationToken)
    {
        var clinic = await repository.GetByIdAsync(id, cancellationToken);
        if (clinic is null) return null;

        var normalizedSlug = NormalizeSlug(slug);
        if (await repository.SlugExistsAsync(normalizedSlug, id, cancellationToken))
            throw new ClinicSlugAlreadyExistsException(normalizedSlug);

        clinic.UpdateDetails(name, normalizedSlug);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(clinic);
    }

    public async Task<ClinicDto?> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var clinic = await repository.GetByIdAsync(id, cancellationToken);
        if (clinic is null) return null;

        clinic.SetActive(isActive);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(clinic);
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required.", nameof(slug));

        return slug.Trim().ToLowerInvariant();
    }

    private static ClinicDto ToDto(Clinic clinic) =>
        new(clinic.Id, clinic.Name, clinic.Slug, clinic.IsActive, clinic.CreatedAtUtc, clinic.UpdatedAtUtc);
}
