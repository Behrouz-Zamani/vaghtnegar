using Microsoft.Extensions.DependencyInjection;
using VaghtNegar.Application.Clinics;
using VaghtNegar.Application.Doctors;
using VaghtNegar.Application.Patients;

namespace VaghtNegar.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClinicService>();
        services.AddScoped<DoctorService>();
        services.AddScoped<PatientService>();
        return services;
    }
}
