using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Application.Doctors;

public interface IDoctorRepository
{
    Task<IReadOnlyList<Doctor>> GetAllAsync(Guid? clinicId, CancellationToken cancellationToken);
    Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Doctor doctor, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}