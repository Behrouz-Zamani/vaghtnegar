using VaghtNegar.Application.Appointments;

namespace VaghtNegar.API.Endpoints;

public static class AppointmentManagementEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/appointments").WithTags("Appointments");

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

        MapStatusAction(group, "confirm", "ConfirmAppointment", "تأیید نوبت", "تأیید");
        MapStatusAction(group, "cancel", "CancelAppointment", "لغو نوبت", "لغو");
        MapStatusAction(group, "complete", "CompleteAppointment", "ثبت انجام نوبت", "تکمیل");
        MapStatusAction(group, "no-show", "MarkAppointmentNoShow", "ثبت عدم مراجعه بیمار", "عدم مراجعه");

        return endpoints;
    }

    private static void MapStatusAction(
        RouteGroupBuilder group,
        string action,
        string name,
        string summary,
        string actionLabel)
    {
        group.MapPatch("/{id:guid}/" + action, async Task<IResult> (
            Guid id,
            AppointmentManagementService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.ChangeStatusAsync(id, action, cancellationToken);
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
