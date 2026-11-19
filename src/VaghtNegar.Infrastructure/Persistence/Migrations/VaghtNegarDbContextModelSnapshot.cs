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
            entity.Property<string>("IdentityUserId").HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<bool>("IsActive").HasColumnType("bit");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("ClinicId", "MedicalCode");
            entity.HasIndex("IdentityUserId").IsUnique().HasFilter("[IdentityUserId] IS NOT NULL");
            entity.ToTable("Doctors");
        });

        modelBuilder.Entity<VaghtNegar.Domain.Entities.Patient>(entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<string>("FirstName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("LastName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("MobileNumber").IsRequired().HasMaxLength(20).HasColumnType("nvarchar(20)");
            entity.Property<DateOnly?>("BirthDate").HasColumnType("date");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("MobileNumber");
            entity.ToTable("Patients");
        });
    }
}
