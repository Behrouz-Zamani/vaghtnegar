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

        group.MapPost("", () =>
            Results.Problem(
                title: "پرداخت حق‌ویزیت الزامی است.",
                detail: "برای دریافت نوبت آنلاین از گردش درخواست نوبت و پرداخت استفاده کنید.",
                statusCode: StatusCodes.Status410Gone))
        .RequireAuthorization(policy => policy.RequireRole("Patient"))
        .WithName("BookDoctorAppointmentLegacy")
        .WithSummary("مسیر رزرو مستقیم بیمار غیرفعال است؛ ثبت درخواست باید از طریق گردش پرداخت انجام شود.");

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
