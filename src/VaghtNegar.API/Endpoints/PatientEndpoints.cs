using VaghtNegar.Application.Patients;

namespace VaghtNegar.API.Endpoints;

public static class PatientEndpoints
{
    private static readonly string[] PatientStaffRoles = ["Receptionist", "ClinicAdmin", "SuperAdmin"];

    public static IEndpointRouteBuilder MapPatientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Patient records contain personally identifiable information. Until a patient-self
        // endpoint is implemented, all patient CRUD and identity-link operations are staff-only.
        var group = endpoints.MapGroup("/api/v1/patients")
            .WithTags("Patients")
            .RequireAuthorization(policy => policy.RequireRole(PatientStaffRoles));

        group.MapGet("/", async (string? search, PatientService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(search, ct)))
            .WithName("GetPatients");

        group.MapGet("/{id:guid}", async (Guid id, PatientService service, CancellationToken ct) =>
        {
            var patient = await service.GetByIdAsync(id, ct);
            return patient is null ? Results.NotFound() : Results.Ok(patient);
        }).WithName("GetPatientById");

        group.MapPost("/", async (CreateOrUpdatePatientRequest request, PatientService service, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors is not null) return Results.ValidationProblem(errors);

            try
            {
                var patient = await service.CreateAsync(request.FirstName, request.LastName, request.MobileNumber, request.BirthDate, ct);
                return Results.Created($"/api/v1/patients/{patient.Id}", patient);
            }
            catch (PatientMobileAlreadyExistsException ex)
            {
                return Results.Conflict(new { message = "این شماره موبایل قبلاً ثبت شده است.", mobileNumber = ex.MobileNumber });
            }
        }).WithName("CreatePatient");

        group.MapPut("/{id:guid}", async (Guid id, CreateOrUpdatePatientRequest request, PatientService service, CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors is not null) return Results.ValidationProblem(errors);

            try
            {
                var patient = await service.UpdateAsync(id, request.FirstName, request.LastName, request.MobileNumber, request.BirthDate, ct);
                return patient is null ? Results.NotFound() : Results.Ok(patient);
            }
            catch (PatientMobileAlreadyExistsException ex)
            {
                return Results.Conflict(new { message = "این شماره موبایل قبلاً ثبت شده است.", mobileNumber = ex.MobileNumber });
            }
        }).WithName("UpdatePatient");

        group.MapPatch("/{id:guid}/active", async (Guid id, SetPatientActiveRequest request, PatientService service, CancellationToken ct) =>
        {
            var patient = await service.SetActiveAsync(id, request.IsActive, ct);
            return patient is null ? Results.NotFound() : Results.Ok(patient);
        }).WithName("SetPatientActive");

        group.MapPost("/{id:guid}/identity-link", async (
            Guid id,
            LinkPatientIdentityRequest request,
            PatientService service,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdentityUserId))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["identityUserId"] = ["شناسه sub کاربر Keycloak الزامی است."]
                });

            try
            {
                var patient = await service.LinkIdentityAccountAsync(
                    id, request.IdentityUserId, mobileNumberVerifiedByStaff: true, ct);

                return patient is null
                    ? Results.NotFound(new { message = "پرونده بیمار یافت نشد." })
                    : Results.Ok(new
                    {
                        patient.Id,
                        patient.FirstName,
                        patient.LastName,
                        identityLinked = true
                    });
            }
            catch (PatientIdentityAlreadyLinkedException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("LinkPatientIdentity")
        .WithSummary("اتصال پرونده بیمار به sub حساب Keycloak پس از احراز شماره موبایل توسط کارمند مجاز.");

        return endpoints;
    }

    private static Dictionary<string, string[]>? Validate(CreateOrUpdatePatientRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.FirstName)) errors["firstName"] = ["نام بیمار الزامی است."];
        else if (request.FirstName.Trim().Length > 100) errors["firstName"] = ["نام حداکثر ۱۰۰ کاراکتر است."];

        if (string.IsNullOrWhiteSpace(request.LastName)) errors["lastName"] = ["نام خانوادگی بیمار الزامی است."];
        else if (request.LastName.Trim().Length > 100) errors["lastName"] = ["نام خانوادگی حداکثر ۱۰۰ کاراکتر است."];

        if (string.IsNullOrWhiteSpace(request.MobileNumber)) errors["mobileNumber"] = ["شماره موبایل الزامی است."];
        else if (request.MobileNumber.Trim().Length > 30) errors["mobileNumber"] = ["شماره موبایل معتبر نیست."];

        if (request.BirthDate.HasValue && request.BirthDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            errors["birthDate"] = ["تاریخ تولد نمی‌تواند در آینده باشد."];

        return errors.Count == 0 ? null : errors;
    }
}

public sealed record CreateOrUpdatePatientRequest(string FirstName, string LastName, string MobileNumber, DateOnly? BirthDate);
public sealed record SetPatientActiveRequest(bool IsActive);
public sealed record LinkPatientIdentityRequest(string IdentityUserId);
