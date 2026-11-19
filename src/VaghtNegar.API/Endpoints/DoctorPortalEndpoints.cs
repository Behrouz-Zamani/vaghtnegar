using System.Security.Claims;
using VaghtNegar.Application.Doctors;
using VaghtNegar.Application.Scheduling;

namespace VaghtNegar.API.Endpoints;

public static class DoctorPortalEndpoints
{
    public static IEndpointRouteBuilder MapDoctorPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/me/doctor")
            .WithTags("Doctor Portal")
            .RequireAuthorization(policy => policy.RequireRole("Doctor"));

        group.MapGet("", async (ClaimsPrincipal user, DoctorService doctors, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            var doctor = await doctors.GetByIdentityUserIdAsync(subject, ct);
            return doctor is null
                ? Results.NotFound(new { message = "حساب کاربری پزشک هنوز به پرونده پزشک متصل نشده است. از مدیر سامانه بخواهید اتصال حساب را انجام دهد." })
                : Results.Ok(doctor);
        }).WithName("GetMyDoctorProfile");

        group.MapGet("/schedule/working-hours", async (
            ClaimsPrincipal user, DoctorService doctors, SchedulingService scheduling, CancellationToken ct) =>
        {
            var doctor = await ResolveDoctorAsync(user, doctors, ct);
            if (doctor is null) return Results.NotFound(new { message = "حساب کاربری پزشک به پرونده پزشک متصل نیست." });
            return Results.Ok(await scheduling.GetWorkingHoursAsync(doctor.Id, ct));
        }).WithName("GetMyDoctorWorkingHours");

        group.MapPut("/schedule/working-hours", async (
            ClaimsPrincipal user, ReplaceWorkingHoursRequest request,
            DoctorService doctors, SchedulingService scheduling, CancellationToken ct) =>
        {
            var doctor = await ResolveDoctorAsync(user, doctors, ct);
            if (doctor is null) return Results.NotFound(new { message = "حساب کاربری پزشک به پرونده پزشک متصل نیست." });
            try
            {
                return Results.Ok(await scheduling.ReplaceWorkingHoursAsync(doctor.Id, request.Items ?? [], ct));
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["schedule"] = [ex.Message] });
            }
        }).WithName("ReplaceMyDoctorWorkingHours");

        group.MapGet("/schedule/available-slots", async (
            ClaimsPrincipal user, string date, DoctorService doctors,
            SchedulingService scheduling, CancellationToken ct) =>
        {
            var doctor = await ResolveDoctorAsync(user, doctors, ct);
            if (doctor is null) return Results.NotFound(new { message = "حساب کاربری پزشک به پرونده پزشک متصل نیست." });
            try
            {
                return Results.Ok(await scheduling.GetAvailableSlotsAsync(doctor.Id, date, ct));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        }).WithName("GetMyDoctorAvailableSlots");

        return endpoints;
    }

    private static Task<DoctorDto?> ResolveDoctorAsync(
        ClaimsPrincipal user, DoctorService doctors, CancellationToken ct)
    {
        var subject = user.FindFirst("sub")?.Value;
        return string.IsNullOrWhiteSpace(subject)
            ? Task.FromResult<DoctorDto?>(null)
            : doctors.GetByIdentityUserIdAsync(subject, ct);
    }
}
