using System.Security.Claims;
using VaghtNegar.Application.Appointments;
using VaghtNegar.Application.Scheduling;

namespace VaghtNegar.API.Endpoints;

public static class AppointmentPaymentEndpoints
{
    private static readonly string[] AdminRoles = ["ClinicAdmin", "SuperAdmin"];

    public static IEndpointRouteBuilder MapAppointmentPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicGroup = endpoints.MapGroup("/api/v1/booking-requests")
            .WithTags("Appointment Booking and Payment");

        publicGroup.MapPost("/{doctorId:guid}", async Task<IResult> (
            Guid doctorId, CreateBookingHoldRequest request,
            IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            try { return Results.Created("/api/v1/booking-requests", await workflow.CreateHoldAsync(doctorId, request, ct)); }
            catch (DoctorForScheduleNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        })
        .AllowAnonymous()
        .WithName("CreateAppointmentBookingHold")
        .WithSummary("ثبت درخواست اولیه و نگه‌داشتن ساعت به مدت ۱۰ دقیقه، پیش از ورود بیمار");

        publicGroup.MapGet("/{token}", async Task<IResult> (
            string token, IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            var result = await workflow.GetByTokenAsync(token, ct);
            return result is null ? Results.NotFound(new { message = "درخواست نوبت یافت نشد." }) : Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("GetAppointmentBookingHold")
        .WithSummary("مشاهده وضعیت درخواست با شناسه پیگیری");

        publicGroup.MapPost("/{token}/claim", async Task<IResult> (
            string token, ClaimsPrincipal user, IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try { return Results.Ok(await workflow.ClaimAsync(token, subject, ct)); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Patient"))
        .WithName("ClaimAppointmentBookingHold")
        .WithSummary("اتصال درخواست موقت به پرونده بیمار پس از ورود یا ثبت‌نام");

        publicGroup.MapPost("/{token}/receipt", async Task<IResult> (
            string token, SubmitPaymentReceiptRequest request, ClaimsPrincipal user,
            IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try { return Results.Ok(await workflow.SubmitReceiptAsync(token, subject, request, ct)); }
            catch (UnauthorizedAccessException ex) { return Results.Forbid(); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Patient"))
        .WithName("SubmitAppointmentPaymentReceipt")
        .WithSummary("ارسال تصویر رسید کارت‌به‌کارت در مهلت نگه‌داری نوبت");

        publicGroup.MapGet("/me", async Task<IResult> (
            ClaimsPrincipal user, int? page, int? pageSize,
            IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            return Results.Ok(await workflow.GetMyRequestsAsync(subject, page ?? 1, pageSize ?? 20, ct));
        })
        .RequireAuthorization(policy => policy.RequireRole("Patient"))
        .WithName("GetMyBookingRequests");

        var patientRequests = endpoints.MapGroup("/api/v1/me/booking-requests")
            .WithTags("Patient Portal - Payments")
            .RequireAuthorization(policy => policy.RequireRole("Patient"));

        patientRequests.MapGet("/{id:guid}", async Task<IResult> (
            Guid id, ClaimsPrincipal user, IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            var result = await workflow.GetMyRequestAsync(id, subject, ct);
            return result is null ? Results.NotFound(new { message = "درخواست نوبت یافت نشد." }) : Results.Ok(result);
        }).WithName("GetMyBookingRequestById");

        patientRequests.MapPost("/{id:guid}/receipt", async Task<IResult> (
            Guid id, SubmitPaymentReceiptRequest request, ClaimsPrincipal user,
            IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try { return Results.Ok(await workflow.SubmitMyReceiptAsync(id, subject, request, ct)); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).WithName("SubmitReceiptForMyBookingRequest");

        var admin = endpoints.MapGroup("/api/v1/admin/booking-requests")
            .WithTags("Appointment Booking and Payment")
            .RequireAuthorization(policy => policy.RequireRole(AdminRoles));

        admin.MapGet("/pending-review", async (
            int? page, int? pageSize, IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
            Results.Ok(await workflow.GetPendingReviewAsync(page ?? 1, pageSize ?? 20, ct)))
            .WithName("GetBookingRequestsPendingReview");

        admin.MapGet("/{id:guid}/receipt", async Task<IResult> (
            Guid id, IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            var receipt = await workflow.GetReceiptAsync(id, ct);
            return receipt is null
                ? Results.NotFound(new { message = "تصویر رسید یافت نشد." })
                : Results.Ok(new
                {
                    contentType = receipt.Value.ContentType,
                    fileName = receipt.Value.FileName,
                    base64Content = Convert.ToBase64String(receipt.Value.Content)
                });
        })
        .WithName("GetBookingPaymentReceipt");

        admin.MapPost("/{id:guid}/approve", async Task<IResult> (
            Guid id, ClaimsPrincipal user, IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try { return Results.Ok(await workflow.ApproveAsync(id, subject, ct)); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        })
        .WithName("ApproveBookingPayment")
        .WithSummary("تأیید رسید و ایجاد نوبت قطعی");

        admin.MapPost("/{id:guid}/reject", async Task<IResult> (
            Guid id, RejectBookingPaymentRequest request, ClaimsPrincipal user,
            IAppointmentPaymentWorkflow workflow, CancellationToken ct) =>
        {
            var subject = user.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return Results.Unauthorized();
            try { return Results.Ok(await workflow.RejectAsync(id, subject, request.Reason, ct)); }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        })
        .WithName("RejectBookingPayment")
        .WithSummary("رد رسید و آزادسازی ساعت");

        return endpoints;
    }
}
