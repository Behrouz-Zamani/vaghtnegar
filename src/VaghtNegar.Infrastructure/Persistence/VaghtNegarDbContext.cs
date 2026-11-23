using Microsoft.EntityFrameworkCore;
using VaghtNegar.Domain.Entities;

namespace VaghtNegar.Infrastructure.Persistence;

public sealed class VaghtNegarDbContext(DbContextOptions<VaghtNegarDbContext> options) : DbContext(options)
{
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentPayment> AppointmentPayments => Set<AppointmentPayment>();
    public DbSet<DoctorWorkingHour> DoctorWorkingHours => Set<DoctorWorkingHour>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Clinic>(entity =>
        {
            entity.ToTable("Clinics");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MedicalCode).HasMaxLength(50);
            entity.Property(x => x.VisitFeeToman).IsRequired();
            entity.Property(x => x.IdentityUserId).HasMaxLength(128);
            entity.HasIndex(x => x.IdentityUserId).IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");
            entity.HasIndex(x => new { x.ClinicId, x.MedicalCode });
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MobileNumber).HasMaxLength(20).IsRequired();
            entity.Property(x => x.IdentityUserId).HasMaxLength(128);
            entity.HasIndex(x => x.MobileNumber).IsUnique();
            entity.HasIndex(x => x.IdentityUserId)
                .IsUnique()
                .HasFilter("[IdentityUserId] IS NOT NULL");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.PatientNote).HasMaxLength(1000);
            entity.HasIndex(x => new { x.DoctorId, x.StartsAtUtc });
            entity.HasIndex(x => new { x.ClinicId, x.StartsAtUtc });
        });

        modelBuilder.Entity<AppointmentPayment>(entity =>
        {
            entity.ToTable("AppointmentPayments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.ReceiptReference).HasMaxLength(100);
            entity.Property(x => x.ReceiptContentType).HasMaxLength(50);
            entity.Property(x => x.ReviewedBy).HasMaxLength(128);
            entity.Property(x => x.ReviewNote).HasMaxLength(500);
            entity.HasIndex(x => x.AppointmentId).IsUnique();
            entity.HasIndex(x => x.ExpiresAtUtc);
            entity.HasIndex(x => new { x.PatientId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.Status, x.ExpiresAtUtc });
            entity.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DoctorWorkingHour>(entity =>
        {
            entity.ToTable("DoctorWorkingHours");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DayOfWeek).HasConversion<int>();
            entity.Property(x => x.StartTime).HasColumnType("time");
            entity.Property(x => x.EndTime).HasColumnType("time");
            entity.HasIndex(x => new { x.DoctorId, x.DayOfWeek, x.StartTime });
            entity.HasOne<Doctor>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}