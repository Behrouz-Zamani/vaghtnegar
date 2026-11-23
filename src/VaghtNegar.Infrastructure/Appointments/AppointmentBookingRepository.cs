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

        return await CreateAppointmentAsync(
            doctor, patient, startsAtUtc, endsAtUtc, patientNote, transaction, cancellationToken);
    }

    public async Task<AppointmentBookingResult> TryBookForPatientAsync(
        Guid doctorId,
        Guid patientId,
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
            .FirstOrDefaultAsync(x => x.Id == patientId && x.IsActive, cancellationToken);

        if (patient is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AppointmentBookingResult(AppointmentBookingFailure.PatientNotFound, null);
        }

        return await CreateAppointmentAsync(
            doctor, patient, startsAtUtc, endsAtUtc, patientNote, transaction, cancellationToken);
    }

    private async Task<AppointmentBookingResult> CreateAppointmentAsync(
        Doctor doctor,
        Patient patient,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        string? patientNote,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var hasOverlap = await db.Appointments.AnyAsync(x =>
            x.DoctorId == doctor.Id &&
            x.Status != AppointmentStatus.Cancelled &&
            x.StartsAtUtc < endsAtUtc &&
            x.EndsAtUtc > startsAtUtc,
            cancellationToken);

        var hasActiveBookingHold = await db.AppointmentBookingRequests.AnyAsync(x =>
            x.DoctorId == doctor.Id &&
            (x.Status == AppointmentBookingRequestStatus.AwaitingReview ||
             ((x.Status == AppointmentBookingRequestStatus.AwaitingLogin ||
               x.Status == AppointmentBookingRequestStatus.AwaitingPayment) &&
              x.HoldExpiresAtUtc > DateTimeOffset.UtcNow)) &&
            x.StartsAtUtc < endsAtUtc && x.EndsAtUtc > startsAtUtc,
            cancellationToken);

        if (hasOverlap || hasActiveBookingHold)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AppointmentBookingResult(AppointmentBookingFailure.SlotUnavailable, null);
        }

        var appointment = new Appointment(
            doctor.ClinicId, doctor.Id, patient.Id, startsAtUtc, endsAtUtc, patientNote);

        await db.Appointments.AddAsync(appointment, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AppointmentBookingResult(AppointmentBookingFailure.None, appointment);
    }
}
