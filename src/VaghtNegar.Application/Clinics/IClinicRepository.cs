using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Application.Clinics;

public interface IClinicRepository
{
    Task<IReadOnlyList<Clinic>> GetAllAsync(CancellationToken cancellationToken);
    Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, Guid? exceptClinicId, CancellationToken cancellationToken);
    Task AddAsync(Clinic clinic, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
