using System.Security.Claims;
using VaghtNegar.Application.Appointments;
using VaghtNegar.Application.Scheduling;

namespace VaghtNegar.API.Endpoints;

public static class AppointmentBookingEndpoints
{
    private static readonly string[] StaffRoles = ["Receptionist", "ClinicAdmin", "SuperAdmin"];

    public static IEndpointRouteBuilder MapAppointmentBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/doctors/{doctorId:guid}/appointments")
            .WithTags("Appointments");

        group.MapPost("", async Task<IResult> (
            Guid doctorId,
            BookAppointmentRequest request,
            ClaimsPrincipal user,
            AppointmentBookingService service,
            CancellationToken cancellationToken) =>
        {
            var identityUserId = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(identityUserId))
                return Results.Unauthorized();

            try
            {
                var result = await service.BookAsync(doctorId, identityUserId, request, cancellationToken);
                return Results.Created($"/api/v1/appointments/{result.Id}", result);
            }
            catch (DoctorForScheduleNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (PatientForBookingNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (AppointmentSlotUnavailableException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole("Patient"))
        .WithName("BookDoctorAppointment")
        .WithSummary("رزرو نوبت برای بیمار واردشده؛ شناسه بیمار از توکن Keycloak استخراج می‌شود.");

        endpoints.MapPost("/api/v1/appointments", async Task<IResult> (
            StaffBookAppointmentRequest request,
            AppointmentBookingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.BookForPatientAsync(
                    request.DoctorId, request, cancellationToken);
                return Results.Created($"/api/v1/appointments/{result.Id}", result);
            }
            catch (DoctorForScheduleNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (PatientForBookingNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (AppointmentSlotUnavailableException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole(StaffRoles))
        .WithName("CreateAppointmentForPatient")
        .WithSummary("ثبت نوبت توسط پذیرش برای یک پرونده بیمار فعال.");

        return endpoints;
    }
}
