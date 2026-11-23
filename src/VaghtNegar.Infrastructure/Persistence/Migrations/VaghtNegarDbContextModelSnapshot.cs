using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using VaghtNegar.Domain.Enums;
using VaghtNegar.Infrastructure.Persistence;

#nullable disable

namespace VaghtNegar.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VaghtNegarDbContext))]
partial class VaghtNegarDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity<VaghtNegar.Domain.Entities.Appointment>(entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<Guid>("ClinicId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("DoctorId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("PatientId").HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("StartsAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset>("EndsAtUtc").HasColumnType("datetimeoffset");
            entity.Property<AppointmentStatus>("Status").HasColumnType("int");
            entity.Property<string>("PatientNote").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("ClinicId", "StartsAtUtc");
            entity.HasIndex("DoctorId", "StartsAtUtc");
            entity.ToTable("Appointments");
        });

        modelBuilder.Entity<VaghtNegar.Domain.Entities.AppointmentPayment>(entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<Guid>("AppointmentId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("PatientId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("DoctorId").HasColumnType("uniqueidentifier");
            entity.Property<long>("AmountToman").HasColumnType("bigint");
            entity.Property<VaghtNegar.Domain.Enums.AppointmentPaymentStatus>("Status").HasColumnType("int");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset>("ExpiresAtUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("ReceiptReference").HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("ReceiptContentType").HasMaxLength(50).HasColumnType("nvarchar(50)");
            entity.Property<string>("ReceiptImageBase64").HasColumnType("nvarchar(max)");
            entity.Property<DateTimeOffset?>("ReceiptSubmittedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset?>("ReviewedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("ReviewedBy").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<string>("ReviewNote").HasMaxLength(500).HasColumnType("nvarchar(500)");
            entity.HasKey("Id");
            entity.HasIndex("AppointmentId").IsUnique();
            entity.HasIndex("ExpiresAtUtc");
            entity.HasIndex("PatientId", "CreatedAtUtc");
            entity.HasIndex("Status", "ExpiresAtUtc");
            entity.ToTable("AppointmentPayments");
        });

        modelBuilder.Entity<VaghtNegar.Domain.Entities.AppointmentPayment>()
            .HasOne("VaghtNegar.Domain.Entities.Appointment", "Appointment")
            .WithMany()
            .HasForeignKey("AppointmentId")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        modelBuilder.Entity<VaghtNegar.Domain.Entities.Clinic>(entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            entity.Property<string>("Slug").IsRequired().HasMaxLength(120).HasColumnType("nvarchar(120)");
            entity.Property<bool>("IsActive").HasColumnType("bit");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("Slug").IsUnique();
            entity.ToTable("Clinics");
        });

        modelBuilder.Entity<VaghtNegar.Domain.Entities.Doctor>(entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<Guid>("ClinicId").HasColumnType("uniqueidentifier");
            entity.Property<string>("FirstName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("LastName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("MedicalCode").HasMaxLength(50).HasColumnType("nvarchar(50)");
            entity.Property<long>("VisitFeeToman").HasColumnType("bigint");
            entity.Property<string>("IdentityUserId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<bool>("IsActive").HasColumnType("bit");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("ClinicId", "MedicalCode");
            entity.HasIndex("IdentityUserId").IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");
            entity.ToTable("Doctors");
        });

        modelBuilder.Entity<VaghtNegar.Domain.Entities.DoctorWorkingHour>(entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<Guid>("DoctorId").HasColumnType("uniqueidentifier");
            entity.Property<DayOfWeek>("DayOfWeek").HasConversion<int>().HasColumnType("int");
            entity.Property<TimeOnly>("StartTime").HasColumnType("time");
            entity.Property<TimeOnly>("EndTime").HasColumnType("time");
            entity.Property<int>("SlotDurationMinutes").HasColumnType("int");
            entity.Property<bool>("IsActive").HasColumnType("bit");
            entity.HasKey("Id");
            entity.HasIndex("DoctorId", "DayOfWeek", "StartTime");
            entity.ToTable("DoctorWorkingHours");
        });

        modelBuilder.Entity<VaghtNegar.Domain.Entities.DoctorWorkingHour>()
            .HasOne<VaghtNegar.Domain.Entities.Doctor>()
            .WithMany()
            .HasForeignKey("DoctorId")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        modelBuilder.Entity<VaghtNegar.Domain.Entities.Patient>(entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<string>("FirstName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("LastName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("MobileNumber").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            entity.Property<DateOnly?>("BirthDate").HasColumnType("date");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("IdentityUserId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<bool>("IsActive").HasColumnType("bit");
            entity.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("IdentityUserId").IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");
            entity.HasIndex("MobileNumber").IsUnique();
            entity.ToTable("Patients");
        });
    }
}
