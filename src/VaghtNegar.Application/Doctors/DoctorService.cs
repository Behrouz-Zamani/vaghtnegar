using VaghtNegar.Application.Clinics;
using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Application.Doctors;

public sealed class DoctorService(IDoctorRepository repository, IClinicRepository clinicRepository)
{
    public async Task<IReadOnlyList<DoctorDto>> GetAllAsync(Guid? clinicId, CancellationToken ct)
    {
        var doctors = await repository.GetAllAsync(clinicId, ct);
        return doctors.Select(ToDto).ToArray();
    }

    public async Task<DoctorDto?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identityUserId)) return null;
        var doctor = await repository.GetByIdentityUserIdAsync(identityUserId, ct);
        return doctor is null ? null : ToDto(doctor);
    }

    public async Task<DoctorDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var doctor = await repository.GetByIdAsync(id, ct);
        return doctor is null ? null : ToDto(doctor);
    }

    public async Task<DoctorDto> CreateAsync(Guid clinicId, string firstName, string lastName, string? medicalCode, long visitFeeToman, CancellationToken ct)
    {
        var clinic = await clinicRepository.GetByIdAsync(clinicId, ct);
        if (clinic is null) throw new ClinicNotFoundException(clinicId);
        if (!clinic.IsActive) throw new ClinicInactiveException(clinicId);
        var doctor = new Doctor(clinicId, firstName, lastName, medicalCode, visitFeeToman);
        await repository.AddAsync(doctor, ct);
        await repository.SaveChangesAsync(ct);
        return ToDto(doctor);
    }

    public async Task<DoctorDto?> UpdateAsync(Guid id, string firstName, string lastName, string? medicalCode, long visitFeeToman, CancellationToken ct)
    {
        var doctor = await repository.GetByIdAsync(id, ct);
        if (doctor is null) return null;
        doctor.UpdateDetails(firstName, lastName, medicalCode, visitFeeToman);
        await repository.SaveChangesAsync(ct);
        return ToDto(doctor);
    }

    public async Task<DoctorDto?> UpdateMyVisitFeeAsync(string identityUserId, long visitFeeToman, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identityUserId)) return null;
        if (visitFeeToman <= 0) throw new ArgumentOutOfRangeException(nameof(visitFeeToman), "حق ویزیت باید بیشتر از صفر باشد.");
        var doctor = await repository.GetByIdentityUserIdAsync(identityUserId, ct);
        if (doctor is null) return null;
        doctor.UpdateVisitFee(visitFeeToman);
        await repository.SaveChangesAsync(ct);
        return ToDto(doctor);
    }

    public async Task<DoctorDto?> SetActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var doctor = await repository.GetByIdAsync(id, ct);
        if (doctor is null) return null;
        doctor.SetActive(isActive);
        await repository.SaveChangesAsync(ct);
        return ToDto(doctor);
    }

    public async Task<DoctorDto?> LinkIdentityAsync(Guid id, string? identityUserId, CancellationToken ct)
    {
        var doctor = await repository.GetByIdAsync(id, ct);
        if (doctor is null) return null;
        if (!string.IsNullOrWhiteSpace(identityUserId) &&
            await repository.IsIdentityUserIdInUseAsync(identityUserId.Trim(), id, ct))
            throw new DoctorIdentityAlreadyLinkedException();
        doctor.LinkIdentity(identityUserId);
        await repository.SaveChangesAsync(ct);
        return ToDto(doctor);
    }

    private static DoctorDto ToDto(Doctor x) => new(x.Id, x.ClinicId, x.FirstName, x.LastName, x.MedicalCode, x.VisitFeeToman, !string.IsNullOrWhiteSpace(x.IdentityUserId), x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc);
}

public sealed class ClinicNotFoundException(Guid clinicId) : Exception($"Clinic '{clinicId}' was not found.")
{
    public Guid ClinicId { get; } = clinicId;
}

public sealed class ClinicInactiveException(Guid clinicId) : Exception($"Clinic '{clinicId}' is inactive.")
{
    public Guid ClinicId { get; } = clinicId;
}
public sealed class DoctorIdentityAlreadyLinkedException() : Exception("این حساب کاربری قبلاً به پزشک دیگری متصل شده است.");
