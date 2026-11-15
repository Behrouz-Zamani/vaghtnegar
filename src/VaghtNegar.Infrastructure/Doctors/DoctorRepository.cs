using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.Doctors;
using VaghtNegar.Domain.Entities;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.Infrastructure.Doctors;

public sealed class DoctorRepository(VaghtNegarDbContext dbContext) : IDoctorRepository
{
    public async Task<IReadOnlyList<Doctor>> GetAllAsync(Guid? clinicId, CancellationToken ct)
    {
        var query = dbContext.Doctors.AsNoTracking().AsQueryable();
        if (clinicId.HasValue) query = query.Where(x => x.ClinicId == clinicId.Value);
        return await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync(ct);
    }

    public Task<Doctor?> GetByIdAsync(Guid id, CancellationToken ct) =>
        dbContext.Doctors.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddAsync(Doctor doctor, CancellationToken ct) =>
        await dbContext.Doctors.AddAsync(doctor, ct);

    public Task SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
}