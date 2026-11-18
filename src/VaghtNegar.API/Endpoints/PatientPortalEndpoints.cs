using System.Security.Claims;
using VaghtNegar.Application.PatientPortal;

namespace VaghtNegar.API.Endpoints;

public static class PatientPortalEndpoints
{
    public static IEndpointRouteBuilder MapPatientPortalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/me")
            .WithTags("Patient Portal")
            .RequireAuthorization(policy => policy.RequireRole("Patient"));

        group.MapGet("/patient", async (
            ClaimsPrincipal user,
            PatientPortalService service,
            CancellationToken cancellationToken) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject))
                return Results.Unauthorized();

            var patient = await service.GetProfileAsync(subject, cancellationToken);
            return patient is null
                ? Results.NotFound(new { message = "پرونده فعال بیمار به حساب کاربری فعلی متصل نیست." })
                : Results.Ok(patient);
        })
        .WithName("GetMyPatientProfile")
        .WithSummary("مشاهده پرونده بیمار واردشده");

        group.MapGet("/appointments", async (
            ClaimsPrincipal user,
            int? page,
            int? pageSize,
            PatientPortalService service,
            CancellationToken cancellationToken) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject))
                return Results.Unauthorized();

            try
            {
                return Results.Ok(await service.GetAppointmentsAsync(
                    subject, page ?? 1, pageSize ?? 20, cancellationToken));
            }
            catch (PatientPortalProfileNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("GetMyAppointments")
        .WithSummary("مشاهده نوبت‌های بیمار واردشده با صفحه‌بندی");

        group.MapPatch("/appointments/{id:guid}/cancel", async (
            Guid id,
            ClaimsPrincipal user,
            PatientPortalService service,
            CancellationToken cancellationToken) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject))
                return Results.Unauthorized();

            try
            {
                var appointment = await service.CancelAppointmentAsync(subject, id, cancellationToken);
                return Results.Ok(appointment);
            }
            catch (PatientPortalAppointmentNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (PatientPortalInvalidAppointmentStateException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("CancelMyAppointment")
        .WithSummary("لغو نوبت متعلق به بیمار واردشده");

        return endpoints;
    }
}
