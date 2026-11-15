using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Application.Patients;

public sealed class PatientService(IPatientRepository repository)
{
    public async Task<IReadOnlyList<PatientDto>> GetAllAsync(string? search, CancellationToken ct)
    {
        var patients = await repository.GetAllAsync(search, ct);
        return patients.Select(ToDto).ToArray();
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var patient = await repository.GetByIdAsync(id, ct);
        return patient is null ? null : ToDto(patient);
    }

    public async Task<PatientDto> CreateAsync(string firstName, string lastName, string mobileNumber, DateOnly? birthDate, CancellationToken ct)
    {
        var normalizedMobile = NormalizeMobile(mobileNumber);
        if (await repository.MobileNumberExistsAsync(normalizedMobile, null, ct))
            throw new PatientMobileAlreadyExistsException(normalizedMobile);

        var patient = new Patient(firstName, lastName, normalizedMobile, birthDate);
        await repository.AddAsync(patient, ct);
        await repository.SaveChangesAsync(ct);
        return ToDto(patient);
    }

    public async Task<PatientDto?> UpdateAsync(Guid id, string firstName, string lastName, string mobileNumber, DateOnly? birthDate, CancellationToken ct)
    {
        var patient = await repository.GetByIdAsync(id, ct);
        if (patient is null) return null;

        var normalizedMobile = NormalizeMobile(mobileNumber);
        if (await repository.MobileNumberExistsAsync(normalizedMobile, id, ct))
            throw new PatientMobileAlreadyExistsException(normalizedMobile);

        patient.UpdateDetails(firstName, lastName, normalizedMobile, birthDate);
        await repository.SaveChangesAsync(ct);
        return ToDto(patient);
    }

    public async Task<PatientDto?> SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var patient = await repository.GetByIdAsync(id, ct);
        if (patient is null) return null;
        patient.SetActive(isActive);
        await repository.SaveChangesAsync(ct);
        return ToDto(patient);
    }

    private static string NormalizeMobile(string mobileNumber) =>
        new((mobileNumber ?? string.Empty).Where(c => !char.IsWhiteSpace(c)).ToArray());

    private static PatientDto ToDto(Patient x) =>
        new(x.Id, x.FirstName, x.LastName, x.MobileNumber, x.BirthDate, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
}

public sealed class PatientMobileAlreadyExistsException(string mobileNumber)
    : Exception($"A patient with mobile number '{mobileNumber}' already exists.")
{
    public string MobileNumber { get; } = mobileNumber;
}
