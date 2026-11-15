using VaghtNegar.Application.Clinics;

namespace VaghtNegar.API.Endpoints;

public static class ClinicEndpoints
{
    public static IEndpointRouteBuilder MapClinicEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/clinics")
            .WithTags("Clinics");

        group.MapGet("/", async (ClinicService service, CancellationToken ct) =>
            Results.Ok(await service.GetAllAsync(ct)))
            .WithName("GetClinics");

        group.MapGet("/{id:guid}", async (Guid id, ClinicService service, CancellationToken ct) =>
        {
            var clinic = await service.GetByIdAsync(id, ct);
            return clinic is null ? Results.NotFound() : Results.Ok(clinic);
        }).WithName("GetClinicById");

        group.MapPost("/", async (CreateOrUpdateClinicRequest request, ClinicService service, CancellationToken ct) =>
        {
            var validation = Validate(request.Name, request.Slug);
            if (validation is not null) return Results.ValidationProblem(validation);

            try
            {
                var clinic = await service.CreateAsync(request.Name, request.Slug, ct);
                return Results.Created($"/api/v1/clinics/{clinic.Id}", clinic);
            }
            catch (ClinicSlugAlreadyExistsException ex)
            {
                return Results.Conflict(new { message = ex.Message, slug = ex.Slug });
            }
        }).WithName("CreateClinic");

        group.MapPut("/{id:guid}", async (Guid id, CreateOrUpdateClinicRequest request, ClinicService service, CancellationToken ct) =>
        {
            var validation = Validate(request.Name, request.Slug);
            if (validation is not null) return Results.ValidationProblem(validation);

            try
            {
                var clinic = await service.UpdateAsync(id, request.Name, request.Slug, ct);
                return clinic is null ? Results.NotFound() : Results.Ok(clinic);
            }
            catch (ClinicSlugAlreadyExistsException ex)
            {
                return Results.Conflict(new { message = ex.Message, slug = ex.Slug });
            }
        }).WithName("UpdateClinic");

        group.MapPatch("/{id:guid}/active", async (Guid id, SetClinicActiveRequest request, ClinicService service, CancellationToken ct) =>
        {
            var clinic = await service.SetActiveAsync(id, request.IsActive, ct);
            return clinic is null ? Results.NotFound() : Results.Ok(clinic);
        }).WithName("SetClinicActive");

        return endpoints;
    }

    private static Dictionary<string, string[]>? Validate(string? name, string? slug)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = ["نام کلینیک الزامی است."];
        else if (name.Trim().Length > 200) errors["name"] = ["نام کلینیک حداکثر ۲۰۰ کاراکتر است."];

        if (string.IsNullOrWhiteSpace(slug)) errors["slug"] = ["شناسه یکتا (slug) الزامی است."];
        else if (slug.Trim().Length > 120) errors["slug"] = ["شناسه یکتا حداکثر ۱۲۰ کاراکتر است."];
        else if (slug.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-')))
            errors["slug"] = ["شناسه یکتا فقط می‌تواند شامل حروف انگلیسی، عدد و خط تیره باشد."];

        return errors.Count == 0 ? null : errors;
    }
}

public sealed record CreateOrUpdateClinicRequest(string Name, string Slug);
public sealed record SetClinicActiveRequest(bool IsActive);
