using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.Patients;
using VaghtNegar.Domain.Entities;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.Infrastructure.Patients;

public sealed class PatientRepository(VaghtNegarDbContext dbContext) : IPatientRepository
{
    public async Task<IReadOnlyList<Patient>> GetAllAsync(string? search, CancellationToken ct)
    {
        var query = dbContext.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.FirstName.Contains(term) ||
                x.LastName.Contains(term) ||
                x.MobileNumber.Contains(term));
        }

        return await query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync(ct);
    }

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct) =>
        dbContext.Patients.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> MobileNumberExistsAsync(string mobileNumber, Guid? exceptPatientId, CancellationToken ct) =>
        dbContext.Patients.AnyAsync(x =>
            x.MobileNumber == mobileNumber &&
            (!exceptPatientId.HasValue || x.Id != exceptPatientId.Value), ct);

    public Task<bool> IdentityUserIdExistsAsync(string identityUserId, Guid? exceptPatientId, CancellationToken ct) =>
        dbContext.Patients.AnyAsync(x =>
            x.IdentityUserId == identityUserId &&
            (!exceptPatientId.HasValue || x.Id != exceptPatientId.Value), ct);

    public async Task AddAsync(Patient patient, CancellationToken ct) =>
        await dbContext.Patients.AddAsync(patient, ct);

    public Task SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
}
