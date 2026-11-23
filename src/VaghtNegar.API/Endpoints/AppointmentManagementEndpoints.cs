using Microsoft.EntityFrameworkCore;
using VaghtNegar.Application.Appointments;
using VaghtNegar.Domain.Enums;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.API.Endpoints;

public static class AppointmentManagementEndpoints
{
    private static readonly string[] AppointmentStaffRoles = ["Receptionist", "ClinicAdmin", "SuperAdmin"];

    public static IEndpointRouteBuilder MapAppointmentManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Appointment details include patient name, mobile number and clinical notes.
        var group = endpoints.MapGroup("/api/v1/appointments")
            .WithTags("Appointments")
            .RequireAuthorization(policy => policy.RequireRole(AppointmentStaffRoles));

        group.MapGet("", async (
            Guid? doctorId,
            Guid? patientId,
            string? status,
            string? fromDate,
            string? toDate,
            int? page,
            int? pageSize,
            AppointmentManagementService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.GetListAsync(
                    doctorId, patientId, status, fromDate, toDate,
                    page ?? 1, pageSize ?? 20, cancellationToken);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("GetAppointments")
        .WithSummary("فهرست نوبت‌ها با فیلتر و صفحه‌بندی");

        group.MapGet("/{id:guid}", async (
            Guid id,
            AppointmentManagementService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.GetByIdAsync(id, cancellationToken);
                return result is null
                    ? Results.NotFound(new { message = "نوبت موردنظر یافت نشد." })
                    : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("GetAppointmentById")
        .WithSummary("جزئیات یک نوبت");

        MapStatusAction(group, "confirm", "ConfirmAppointment", "تأیید نوبت");
        MapStatusAction(group, "cancel", "CancelAppointment", "لغو نوبت");
        MapStatusAction(group, "complete", "CompleteAppointment", "ثبت انجام نوبت");
        MapStatusAction(group, "no-show", "MarkAppointmentNoShow", "ثبت عدم مراجعه بیمار");

        return endpoints;
    }

    private static void MapStatusAction(
        RouteGroupBuilder group,
        string action,
        string name,
        string summary)
    {
        group.MapPatch("/{id:guid}/" + action, async Task<IResult> (
            Guid id,
            AppointmentManagementService service,
            VaghtNegarDbContext db,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (action == "confirm")
                {
                    var payment = await db.AppointmentPayments.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.AppointmentId == id, cancellationToken);
                    if (payment is not null && payment.Status != AppointmentPaymentStatus.Approved)
                        return Results.Conflict(new { message = "این نوبت آنلاین است و تا تأیید رسید پرداخت، امکان قطعی‌کردن آن وجود ندارد." });
                }
                var result = await service.ChangeStatusAsync(id, action, cancellationToken);
                if (action == "cancel")
                {
                    var payment = await db.AppointmentPayments.FirstOrDefaultAsync(x => x.AppointmentId == id, cancellationToken);
                    payment?.CancelBeforeApproval("staff");
                    if (payment is not null) await db.SaveChangesAsync(cancellationToken);
                }
                return Results.Ok(result);
            }
            catch (AppointmentForManagementNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidAppointmentStatusTransitionException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName(name)
        .WithSummary(summary);
    }
}
