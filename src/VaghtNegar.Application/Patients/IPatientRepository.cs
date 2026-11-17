using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Application.Patients;

public interface IPatientRepository
{
    Task<IReadOnlyList<Patient>> GetAllAsync(string? search, CancellationToken cancellationToken);
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> MobileNumberExistsAsync(string mobileNumber, Guid? exceptPatientId, CancellationToken cancellationToken);
    Task<bool> IdentityUserIdExistsAsync(string identityUserId, Guid? exceptPatientId, CancellationToken cancellationToken);
    Task AddAsync(Patient patient, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
