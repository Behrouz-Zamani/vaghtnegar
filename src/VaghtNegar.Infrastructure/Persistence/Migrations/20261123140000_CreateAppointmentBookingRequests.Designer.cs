using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using VaghtNegar.Domain.Enums;
using VaghtNegar.Infrastructure.Persistence;

#nullable disable

namespace VaghtNegar.Infrastructure.Persistence.Migrations;

partial class CreateAppointmentBookingRequests : Migration
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity<VaghtNegar.Domain.Entities.AppointmentBookingRequest>(entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<Guid>("ClinicId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("DoctorId").HasColumnType("uniqueidentifier");
            entity.Property<Guid?>("PatientId").HasColumnType("uniqueidentifier");
            entity.Property<string>("IdentityUserId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<DateTimeOffset>("StartsAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset>("EndsAtUtc").HasColumnType("datetimeoffset");
            entity.Property<long>("VisitFeeToman").HasColumnType("bigint");
            entity.Property<string>("PatientNote").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property<string>("AccessTokenHash").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<AppointmentBookingRequestStatus>("Status").HasColumnType("int");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset>("HoldExpiresAtUtc").HasColumnType("datetimeoffset");
            entity.Property<byte[]>("ReceiptContent").HasColumnType("varbinary(max)");
            entity.Property<string>("ReceiptContentType").HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("ReceiptFileName").HasMaxLength(180).HasColumnType("nvarchar(180)");
            entity.Property<DateTimeOffset?>("ReceiptSubmittedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("AdminNote").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property<DateTimeOffset?>("ReviewedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("ReviewedByIdentityUserId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.HasKey("Id");
            entity.HasIndex("AccessTokenHash").IsUnique();
            entity.HasIndex("DoctorId", "StartsAtUtc", "Status");
            entity.HasIndex("PatientId", "CreatedAtUtc");
            entity.ToTable("AppointmentBookingRequests");
        });

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
