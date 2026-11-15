using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VaghtNegar.Application.Clinics;
using VaghtNegar.Infrastructure.Clinics;
using VaghtNegar.Infrastructure.Persistence;

namespace VaghtNegar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not configured.");

        services.AddDbContext<VaghtNegarDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IClinicRepository, ClinicRepository>();
        return services;
    }
}
