using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.Clinics;
using VaghtNegar.Domain.Entities;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.Infrastructure.Clinics;

public sealed class ClinicRepository(VaghtNegarDbContext dbContext) : IClinicRepository
{
    public async Task<IReadOnlyList<Clinic>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Clinics.AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Clinics.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptClinicId, CancellationToken cancellationToken) =>
        dbContext.Clinics.AnyAsync(
            x => x.Slug == slug && (!exceptClinicId.HasValue || x.Id != exceptClinicId.Value),
            cancellationToken);

    public async Task AddAsync(Clinic clinic, CancellationToken cancellationToken) =>
        await dbContext.Clinics.AddAsync(clinic, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
