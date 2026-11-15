using VaghtNegar.API.Endpoints;
using VaghtNegar.Application;
using VaghtNegar.Infrastructure;
using VaghtNegar.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// OpenAPI JSON and interactive Swagger UI are available in Development.
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
    version = "0.2.0",
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

app.MapClinicEndpoints();

app.Run();

public partial class Program { }
