using VaghtNegar.Domain.Entities;
using VaghtNegar.Domain.Enums;

namespace VaghtNegar.Application.Appointments;

public interface IAppointmentManagementRepository
{
    Task<PagedAppointmentsDto> GetListAsync(
        Guid? doctorId,
        Guid? patientId,
        AppointmentStatus? status,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<AppointmentManagementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AppointmentManagementDto?> ChangeStatusAsync(
        Guid id,
        AppointmentStatus targetStatus,
        CancellationToken cancellationToken);
}
