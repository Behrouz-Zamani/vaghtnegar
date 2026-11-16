using VaghtNegar.Domain.Enums;

namespace VaghtNegar.Domain.Entities;

public sealed class Appointment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ClinicId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid PatientId { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Pending;
    public string? PatientNote { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private Appointment() { }

    public Appointment(Guid clinicId, Guid doctorId, Guid patientId, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string? patientNote = null)
    {
        if (clinicId == Guid.Empty || doctorId == Guid.Empty || patientId == Guid.Empty)
            throw new ArgumentException("Clinic, doctor and patient are required.");
        if (startsAtUtc.Offset != TimeSpan.Zero || endsAtUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("Appointment times must be UTC.");
        if (startsAtUtc <= DateTimeOffset.UtcNow)
            throw new ArgumentException("Appointment must be in the future.", nameof(startsAtUtc));
        if (endsAtUtc <= startsAtUtc)
            throw new ArgumentException("End time must be after start time.", nameof(endsAtUtc));
        if (patientNote?.Length > 1000)
            throw new ArgumentException("Patient note cannot exceed 1000 characters.", nameof(patientNote));

        ClinicId = clinicId;
        DoctorId = doctorId;
        PatientId = patientId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        PatientNote = string.IsNullOrWhiteSpace(patientNote) ? null : patientNote.Trim();
    }

    public void Confirm()
    {
        if (Status != AppointmentStatus.Pending)
            throw new InvalidOperationException("Only pending appointments can be confirmed.");
        if (StartsAtUtc <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("A past or started appointment cannot be confirmed.");
        Status = AppointmentStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status is not (AppointmentStatus.Pending or AppointmentStatus.Confirmed))
            throw new InvalidOperationException("Only pending or confirmed appointments can be cancelled.");
        if (StartsAtUtc <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("A started or past appointment cannot be cancelled.");
        Status = AppointmentStatus.Cancelled;
    }

    public void Complete()
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed appointments can be completed.");
        if (EndsAtUtc > DateTimeOffset.UtcNow)
            throw new InvalidOperationException("An appointment cannot be completed before its scheduled end time.");
        Status = AppointmentStatus.Completed;
    }

    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed appointments can be marked as no-show.");
        if (EndsAtUtc > DateTimeOffset.UtcNow)
            throw new InvalidOperationException("An appointment cannot be marked as no-show before its scheduled end time.");
        Status = AppointmentStatus.NoShow;
    }
}
