using VaghtNegar.Application.Doctors;

namespace VaghtNegar.API.Endpoints;

public static class DoctorEndpoints
{
    private static readonly string[] AdminRoles = ["ClinicAdmin", "SuperAdmin"];

    public static IEndpointRouteBuilder MapDoctorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/doctors").WithTags("Doctors");

        // Public read endpoints are used by appointment discovery.
        group.MapGet("/", async (Guid? clinicId, DoctorService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(clinicId, ct))).WithName("GetDoctors");

        group.MapGet("/{id:guid}", async (Guid id, DoctorService service, CancellationToken ct) =>
        {
            var doctor = await service.GetByIdAsync(id, ct);
            return doctor is null ? Results.NotFound() : Results.Ok(doctor);
        }).WithName("GetDoctorById");

        group.MapPost("/", async (CreateDoctorRequest request, DoctorService service, CancellationToken ct) =>
        {
            var errors = Validate(request.FirstName, request.LastName, request.MedicalCode, request.VisitFeeToman);
            if (errors is not null) return Results.ValidationProblem(errors);
            if (request.ClinicId == Guid.Empty)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["clinicId"] = ["شناسه کلینیک الزامی است."] });
            try
            {
                var doctor = await service.CreateAsync(request.ClinicId, request.FirstName, request.LastName, request.MedicalCode, request.VisitFeeToman, ct);
                return Results.Created($"/api/v1/doctors/{doctor.Id}", doctor);
            }
            catch (ClinicNotFoundException ex)
            {
                return Results.NotFound(new { message = "کلینیک موردنظر یافت نشد.", clinicId = ex.ClinicId });
            }
            catch (ClinicInactiveException ex)
            {
                return Results.Conflict(new { message = "امکان ثبت پزشک در کلینیک غیرفعال وجود ندارد.", clinicId = ex.ClinicId });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole(AdminRoles))
        .WithName("CreateDoctor");

        group.MapPut("/{id:guid}", async (Guid id, UpdateDoctorRequest request, DoctorService service, CancellationToken ct) =>
        {
            var errors = Validate(request.FirstName, request.LastName, request.MedicalCode);
            if (errors is not null) return Results.ValidationProblem(errors);
            var doctor = await service.UpdateAsync(id, request.FirstName, request.LastName, request.MedicalCode, request.VisitFeeToman, ct);
            return doctor is null ? Results.NotFound() : Results.Ok(doctor);
        })
        .RequireAuthorization(policy => policy.RequireRole(AdminRoles))
        .WithName("UpdateDoctor");

        group.MapPut("/{id:guid}/identity", async (Guid id, LinkDoctorIdentityRequest request, DoctorService service, CancellationToken ct) =>
        {
            if (request.IdentityUserId?.Length > 128)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["identityUserId"] = ["شناسه حساب حداکثر ۱۲۸ کاراکتر است."] });
            try
            {
                var doctor = await service.LinkIdentityAsync(id, request.IdentityUserId, ct);
                return doctor is null ? Results.NotFound() : Results.Ok(doctor);
            }
            catch (DoctorIdentityAlreadyLinkedException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
        })
        .RequireAuthorization(policy => policy.RequireRole(AdminRoles))
        .WithName("LinkDoctorIdentity");

        group.MapPatch("/{id:guid}/active", async (Guid id, SetDoctorActiveRequest request, DoctorService service, CancellationToken ct) =>
        {
            var doctor = await service.SetActiveAsync(id, request.IsActive, ct);
            return doctor is null ? Results.NotFound() : Results.Ok(doctor);
        })
        .RequireAuthorization(policy => policy.RequireRole(AdminRoles))
        .WithName("SetDoctorActive");

        return endpoints;
    }

    private static Dictionary<string, string[]>? Validate(string? firstName, string? lastName, string? medicalCode, long visitFeeToman)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(firstName)) errors["firstName"] = ["نام پزشک الزامی است."];
        else if (firstName.Trim().Length > 100) errors["firstName"] = ["نام حداکثر ۱۰۰ کاراکتر است."];
        if (string.IsNullOrWhiteSpace(lastName)) errors["lastName"] = ["نام خانوادگی پزشک الزامی است."];
        else if (lastName.Trim().Length > 100) errors["lastName"] = ["نام خانوادگی حداکثر ۱۰۰ کاراکتر است."];
        if (!string.IsNullOrWhiteSpace(medicalCode) && medicalCode.Trim().Length > 50)
            errors["medicalCode"] = ["کد نظام پزشکی حداکثر ۵۰ کاراکتر است."];
        if (visitFeeToman < 0)
            errors["visitFeeToman"] = ["مبلغ حق ویزیت نمی‌تواند منفی باشد."];
        return errors.Count == 0 ? null : errors;
    }
}

public sealed record CreateDoctorRequest(Guid ClinicId, string FirstName, string LastName, string? MedicalCode, long VisitFeeToman);
public sealed record UpdateDoctorRequest(string FirstName, string LastName, string? MedicalCode, long VisitFeeToman);
public sealed record SetDoctorActiveRequest(bool IsActive);

public sealed record LinkDoctorIdentityRequest(string? IdentityUserId);
