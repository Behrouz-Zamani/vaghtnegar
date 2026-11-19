namespace VaghtNegar.Application.Appointments;

public sealed record BookAppointmentRequest(DateTimeOffset StartsAtUtc, string? PatientNote);
public sealed record StaffBookAppointmentRequest(Guid PatientId, DateTimeOffset StartsAtUtc, string? PatientNote);

public sealed record AppointmentBookingDto(
    Guid Id,
    Guid ClinicId,
    Guid DoctorId,
    Guid PatientId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    string? PatientNote,
    DateTimeOffset CreatedAtUtc);

public enum AppointmentBookingFailure
{
    None = 0,
    DoctorNotFound = 1,
    PatientNotFound = 2,
    SlotUnavailable = 3
}

public sealed record AppointmentBookingResult(
    AppointmentBookingFailure Failure,
    VaghtNegar.Domain.Entities.Appointment? Appointment);
