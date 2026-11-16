using System.Data;
using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.Appointments;
using VaghtNegar.Domain.Entities;
using VaghtNegar.Domain.Enums;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.Infrastructure.Appointments;

public sealed class AppointmentManagementRepository(VaghtNegarDbContext db) : IAppointmentManagementRepository
{
    public async Task<PagedAppointmentsDto> GetListAsync(
        Guid? doctorId,
        Guid? patientId,
        AppointmentStatus? status,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = BuildDetailsQuery();

        if (doctorId.HasValue) query = query.Where(x => x.DoctorId == doctorId.Value);
        if (patientId.HasValue) query = query.Where(x => x.PatientId == patientId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value.ToString());
        if (fromUtc.HasValue) query = query.Where(x => x.StartsAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(x => x.StartsAtUtc < toUtc.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.StartsAtUtc)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedAppointmentsDto(items, page, pageSize, totalCount, totalPages);
    }

    public Task<AppointmentManagementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        BuildDetailsQuery().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<AppointmentManagementDto?> ChangeStatusAsync(
        Guid id,
        AppointmentStatus targetStatus,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var appointment = await db.Appointments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (appointment is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        switch (targetStatus)
        {
            case AppointmentStatus.Confirmed:
                appointment.Confirm();
                break;
            case AppointmentStatus.Cancelled:
                appointment.Cancel();
                break;
            case AppointmentStatus.Completed:
                appointment.Complete();
                break;
            case AppointmentStatus.NoShow:
                appointment.MarkNoShow();
                break;
            default:
                throw new InvalidOperationException("تغییر به این وضعیت مجاز نیست.");
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await BuildDetailsQuery().FirstAsync(x => x.Id == id, cancellationToken);
    }

    private IQueryable<AppointmentManagementDto> BuildDetailsQuery() =>
        from appointment in db.Appointments.AsNoTracking()
        join doctor in db.Doctors.AsNoTracking() on appointment.DoctorId equals doctor.Id
        join patient in db.Patients.AsNoTracking() on appointment.PatientId equals patient.Id
        select new AppointmentManagementDto(
            appointment.Id,
            appointment.ClinicId,
            appointment.DoctorId,
            doctor.FirstName + " " + doctor.LastName,
            appointment.PatientId,
            patient.FirstName + " " + patient.LastName,
            patient.MobileNumber,
            appointment.StartsAtUtc,
            appointment.EndsAtUtc,
            appointment.Status.ToString(),
            appointment.PatientNote,
            appointment.CreatedAtUtc);
}
