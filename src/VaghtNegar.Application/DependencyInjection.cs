using Microsoft.Extensions.DependencyInjection;
using VaghtNegar.Application.Clinics;
using VaghtNegar.Application.Doctors;

namespace VaghtNegar.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClinicService>();
        services.AddScoped<DoctorService>();
        return services;
    }
}