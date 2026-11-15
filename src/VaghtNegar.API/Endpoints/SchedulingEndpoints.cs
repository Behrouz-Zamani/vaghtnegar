using VaghtNegar.Application.Scheduling;

namespace VaghtNegar.API.Endpoints;

public static class SchedulingEndpoints
{
    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/doctors/{doctorId:guid}/schedule").WithTags("Doctor Scheduling");

        group.MapGet("/working-hours", async (Guid doctorId, SchedulingService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetWorkingHoursAsync(doctorId, ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("GetDoctorWorkingHours");

        group.MapPut("/working-hours", async (Guid doctorId, ReplaceWorkingHoursRequest request, SchedulingService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.ReplaceWorkingHoursAsync(doctorId, request.Items ?? [], ct)); }
            catch (DoctorForScheduleNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["schedule"] = [ex.Message] }); }
        }).WithName("ReplaceDoctorWorkingHours");

        group.MapGet("/available-slots", async (Guid doctorId, string date, SchedulingService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.GetAvailableSlotsAsync(doctorId, date, ct)); }
            catch (DoctorForScheduleNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("GetDoctorAvailableSlots");

        return endpoints;
    }
}

public sealed record ReplaceWorkingHoursRequest(IReadOnlyList<WorkingHourInput>? Items);