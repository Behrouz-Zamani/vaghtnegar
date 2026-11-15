namespace VaghtNegar.Application.Clinics;

public sealed class ClinicSlugAlreadyExistsException(string slug)
    : Exception($"A clinic with slug '{slug}' already exists.")
{
    public string Slug { get; } = slug;
}
