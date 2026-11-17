using System.Data;
using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.Appointments;
using VaghtNegar.Domain.Entities;
using VaghtNegar.Domain.Enums;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.Infrastructure.Appointments;

public sealed class AppointmentBookingRepository(VaghtNegarDbContext db) : IAppointmentBookingRepository
{
    public async Task<AppointmentBookingResult> TryBookAsync(
        Guid doctorId,
        string identityUserId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        string? patientNote,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var doctor = await db.Doctors
            .FirstOrDefaultAsync(x => x.Id == doctorId && x.IsActive, cancellationToken);

        if (doctor is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AppointmentBookingResult(AppointmentBookingFailure.DoctorNotFound, null);
        }

        var patient = await db.Patients
            .FirstOrDefaultAsync(x =>
                x.IdentityUserId == identityUserId && x.IsActive,
                cancellationToken);

        if (patient is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AppointmentBookingResult(AppointmentBookingFailure.PatientNotFound, null);
        }

        var hasOverlap = await db.Appointments.AnyAsync(x =>
            x.DoctorId == doctorId &&
            x.Status != AppointmentStatus.Cancelled &&
            x.StartsAtUtc < endsAtUtc &&
            x.EndsAtUtc > startsAtUtc,
            cancellationToken);

        if (hasOverlap)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AppointmentBookingResult(AppointmentBookingFailure.SlotUnavailable, null);
        }

        var appointment = new Appointment(
            doctor.ClinicId, doctorId, patient.Id, startsAtUtc, endsAtUtc, patientNote);

        await db.Appointments.AddAsync(appointment, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AppointmentBookingResult(AppointmentBookingFailure.None, appointment);
    }
}
