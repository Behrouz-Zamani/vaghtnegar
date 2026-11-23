using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Application.Scheduling;

public interface ISchedulingRepository
{
    Task<bool> DoctorExistsAsync(Guid doctorId, CancellationToken ct);
    Task<IReadOnlyList<DoctorWorkingHour>> GetWorkingHoursAsync(Guid doctorId, CancellationToken ct);
    Task ReplaceWorkingHoursAsync(Guid doctorId, IReadOnlyList<DoctorWorkingHour> hours, CancellationToken ct);
    Task<IReadOnlyList<Appointment>> GetAppointmentsForDayAsync(Guid doctorId, DateTimeOffset dayStartUtc, DateTimeOffset dayEndUtc, CancellationToken ct);
    Task<IReadOnlyList<AppointmentBookingRequest>> GetBookingRequestsForDayAsync(Guid doctorId, DateTimeOffset dayStartUtc, DateTimeOffset dayEndUtc, CancellationToken ct);
}