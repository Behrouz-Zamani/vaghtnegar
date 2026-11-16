using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VaghtNegar.API.Endpoints;
using VaghtNegar.Application;
using VaghtNegar.Infrastructure;
using VaghtNegar.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var keycloakAuthority = builder.Configuration["Keycloak:Authority"]
    ?? "http://localhost:8080/realms/vaghtnegar";
var keycloakAudience = builder.Configuration["Keycloak:Audience"]
    ?? "vaghtnegar-api";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.Audience = keycloakAudience;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakAuthority,
            ValidateAudience = true,
            ValidAudience = keycloakAudience,
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(60)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                var realmAccess = context.Principal?.FindFirst("realm_access")?.Value;

                if (identity is not null && !string.IsNullOrWhiteSpace(realmAccess))
                {
                    try
                    {
                        using var document = JsonDocument.Parse(realmAccess);
                        if (document.RootElement.TryGetProperty("roles", out var roles) &&
                            roles.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var role in roles.EnumerateArray())
                            {
                                if (role.ValueKind != JsonValueKind.String) continue;
                                var roleName = role.GetString();
                                if (!string.IsNullOrWhiteSpace(roleName) &&
                                    !identity.HasClaim(ClaimTypes.Role, roleName))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        context.Fail("The realm_access claim is malformed.");
                    }
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "VaghtNegar API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "VaghtNegar API";
    });
}

app.MapGet("/", () => Results.Ok(new
{
    application = "VaghtNegar",
    status = "running",
    version = "0.7.0",
    documentation = "/swagger"
})).WithName("GetApiInfo");

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
    .WithName("Liveness");

app.MapGet("/health/ready", async (VaghtNegarDbContext db, CancellationToken cancellationToken) =>
{
    var canConnect = await db.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { status = "Ready", database = "Connected" })
        : Results.Problem(title: "Database unavailable", statusCode: StatusCodes.Status503ServiceUnavailable);
}).WithName("Readiness");

app.MapGet("/api/v1/auth/me", (ClaimsPrincipal user) =>
{
    var roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToArray();
    return Results.Ok(new
    {
        subject = user.FindFirst("sub")?.Value,
        username = user.FindFirst("preferred_username")?.Value,
        email = user.FindFirst("email")?.Value,
        emailVerified = bool.TryParse(user.FindFirst("email_verified")?.Value, out var verified) && verified,
        roles
    });
})
.RequireAuthorization()
.WithTags("Authentication")
.WithName("GetCurrentUser")
.WithSummary("اطلاعات هویت کاربر واردشده");

app.MapClinicEndpoints();
app.MapDoctorEndpoints();
app.MapPatientEndpoints();
app.MapSchedulingEndpoints();
app.MapAppointmentBookingEndpoints();
app.MapAppointmentManagementEndpoints();

app.Run();

public partial class Program { }