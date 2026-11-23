using System.Data;
using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.PatientPortal;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.Infrastructure.PatientPortal;

public sealed class PatientPortalRepository(VaghtNegarDbContext db) : IPatientPortalRepository
{
    public Task<PatientProfileDto?> GetProfileAsync(string identityUserId, CancellationToken cancellationToken) =>
        db.Patients
            .AsNoTracking()
            .Where(patient => patient.IdentityUserId == identityUserId && patient.IsActive)
            .Select(patient => new PatientProfileDto(
                patient.Id,
                patient.FirstName,
                patient.LastName,
                patient.MobileNumber,
                patient.BirthDate))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PatientAppointmentsPageDto?> GetAppointmentsAsync(
        string identityUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var patientId = await db.Patients
            .AsNoTracking()
            .Where(patient => patient.IdentityUserId == identityUserId && patient.IsActive)
            .Select(patient => (Guid?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!patientId.HasValue) return null;

        var query =
            from appointment in db.Appointments.AsNoTracking()
            join doctor in db.Doctors.AsNoTracking() on appointment.DoctorId equals doctor.Id
            where appointment.PatientId == patientId.Value
            select new { Appointment = appointment, Doctor = doctor };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Appointment.StartsAtUtc)
            .ThenByDescending(x => x.Appointment.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PatientAppointmentDto(
                x.Appointment.Id,
                x.Appointment.ClinicId,
                x.Appointment.DoctorId,
                x.Doctor.FirstName + " " + x.Doctor.LastName,
                x.Appointment.StartsAtUtc,
                x.Appointment.EndsAtUtc,
                x.Appointment.Status.ToString(),
                x.Appointment.PatientNote,
                x.Appointment.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PatientAppointmentsPageDto(
            items, page, pageSize, totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<PatientAppointmentDto?> CancelAppointmentAsync(
        string identityUserId,
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var patientId = await db.Patients
            .Where(patient => patient.IdentityUserId == identityUserId && patient.IsActive)
            .Select(patient => (Guid?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (!patientId.HasValue)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var appointment = await db.Appointments
            .FirstOrDefaultAsync(
                item => item.Id == appointmentId && item.PatientId == patientId.Value,
                cancellationToken);

        if (appointment is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        appointment.Cancel();
        var payment = await db.AppointmentPayments.FirstOrDefaultAsync(x => x.AppointmentId == appointmentId, cancellationToken);
        payment?.CancelBeforeApproval("patient");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await (
            from item in db.Appointments.AsNoTracking()
            join doctor in db.Doctors.AsNoTracking() on item.DoctorId equals doctor.Id
            where item.Id == appointmentId && item.PatientId == patientId.Value
            select new PatientAppointmentDto(
                item.Id,
                item.ClinicId,
                item.DoctorId,
                doctor.FirstName + " " + doctor.LastName,
                item.StartsAtUtc,
                item.EndsAtUtc,
                item.Status.ToString(),
                item.PatientNote,
                item.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
