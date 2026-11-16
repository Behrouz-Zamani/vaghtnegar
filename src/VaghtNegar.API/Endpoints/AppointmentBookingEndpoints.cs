using VaghtNegar.Application.Appointments;
using VaghtNegar.Application.Scheduling;

namespace VaghtNegar.API.Endpoints;

public static class AppointmentBookingEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/doctors/{doctorId:guid}/appointments")
            .WithTags("Appointments");

        group.MapPost("", async Task<IResult> (
            Guid doctorId,
            BookAppointmentRequest request,
            AppointmentBookingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.BookAsync(doctorId, request, cancellationToken);
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
        .WithName("BookDoctorAppointment")
        .WithSummary("رزرو یک نوبت آزاد برای بیمار");

        return endpoints;
    }
}
