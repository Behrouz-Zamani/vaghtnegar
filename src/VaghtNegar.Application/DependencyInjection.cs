using Microsoft.Extensions.DependencyInjection;
using VaghtNegar.Application.Clinics;

namespace VaghtNegar.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClinicService>();
        return services;
    }
}
