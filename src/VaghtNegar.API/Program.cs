using VaghtNegar.Application;
using VaghtNegar.Infrastructure;
using VaghtNegar.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    application = "VaghtNegar",
    status = "running",
    version = "0.1.0"
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

app.Run();

public partial class Program;
