using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.Scheduling;
using VaghtNegar.Domain.Entities;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.Infrastructure.Scheduling;

public sealed class SchedulingRepository(VaghtNegarDbContext db) : ISchedulingRepository
{
    public Task<bool> DoctorExistsAsync(Guid doctorId, CancellationToken ct) =>
        db.Doctors.AnyAsync(x => x.Id == doctorId && x.IsActive, ct);

    public async Task<IReadOnlyList<DoctorWorkingHour>> GetWorkingHoursAsync(Guid doctorId, CancellationToken ct) =>
        await db.DoctorWorkingHours.AsNoTracking().Where(x => x.DoctorId == doctorId).ToListAsync(ct);

    public async Task ReplaceWorkingHoursAsync(Guid doctorId, IReadOnlyList<DoctorWorkingHour> hours, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var existing = await db.DoctorWorkingHours.Where(x => x.DoctorId == doctorId).ToListAsync(ct);
        db.DoctorWorkingHours.RemoveRange(existing);
        await db.DoctorWorkingHours.AddRangeAsync(hours, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<Appointment>> GetAppointmentsForDayAsync(Guid doctorId, DateTimeOffset dayStartUtc, DateTimeOffset dayEndUtc, CancellationToken ct) =>
        await db.Appointments.AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.StartsAtUtc < dayEndUtc && x.EndsAtUtc > dayStartUtc)
            .ToListAsync(ct);
}