using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VaghtNegar.Application.Appointments;
using VaghtNegar.Application.Clinics;
using VaghtNegar.Application.Doctors;
using VaghtNegar.Application.Patients;
using VaghtNegar.Application.PatientPortal;
using VaghtNegar.Application.Scheduling;
using VaghtNegar.Infrastructure.Appointments;
using VaghtNegar.Infrastructure.Clinics;
using VaghtNegar.Infrastructure.Doctors;
using VaghtNegar.Infrastructure.Patients;
using VaghtNegar.Infrastructure.PatientPortal;
using VaghtNegar.Infrastructure.Scheduling;
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
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<ISchedulingRepository, SchedulingRepository>();
        services.AddScoped<IAppointmentBookingRepository, AppointmentBookingRepository>();
        services.AddScoped<IAppointmentManagementRepository, AppointmentManagementRepository>();
        services.AddScoped<IPatientPortalRepository, PatientPortalRepository>();
        return services;
    }
}