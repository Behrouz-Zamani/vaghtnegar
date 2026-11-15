using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VaghtNegar.Infrastructure.Persistence;

#nullable disable

namespace VaghtNegar.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VaghtNegarDbContext))]
[Migration("202611150001_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Clinics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Clinics", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Doctors",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                MedicalCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Doctors", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Patients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                BirthDate = table.Column<DateOnly>(type: "date", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Patients", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Appointments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                PatientNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Appointments", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_Clinics_Slug", table: "Clinics", column: "Slug", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Doctors_ClinicId_MedicalCode", table: "Doctors", columns: new[] { "ClinicId", "MedicalCode" });
        migrationBuilder.CreateIndex(name: "IX_Patients_MobileNumber", table: "Patients", column: "MobileNumber");
        migrationBuilder.CreateIndex(name: "IX_Appointments_DoctorId_StartsAtUtc", table: "Appointments", columns: new[] { "DoctorId", "StartsAtUtc" });
        migrationBuilder.CreateIndex(name: "IX_Appointments_ClinicId_StartsAtUtc", table: "Appointments", columns: new[] { "ClinicId", "StartsAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Appointments");
        migrationBuilder.DropTable(name: "Clinics");
        migrationBuilder.DropTable(name: "Doctors");
        migrationBuilder.DropTable(name: "Patients");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("VaghtNegar.Domain.Entities.Appointment", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<Guid>("ClinicId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("DoctorId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("PatientId").HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("StartsAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset>("EndsAtUtc").HasColumnType("datetimeoffset");
            entity.Property<int>("Status").HasColumnType("int");
            entity.Property<string>("PatientNote").HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("ClinicId", "StartsAtUtc");
            entity.HasIndex("DoctorId", "StartsAtUtc");
            entity.ToTable("Appointments");
        });

        modelBuilder.Entity("VaghtNegar.Domain.Entities.Clinic", entity =>
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

        modelBuilder.Entity("VaghtNegar.Domain.Entities.Doctor", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("uniqueidentifier");
            entity.Property<Guid>("ClinicId").HasColumnType("uniqueidentifier");
            entity.Property<string>("FirstName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("LastName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            entity.Property<string>("MedicalCode").HasMaxLength(50).HasColumnType("nvarchar(50)");
            entity.Property<bool>("IsActive").HasColumnType("bit");
            entity.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("ClinicId", "MedicalCode");
            entity.ToTable("Doctors");
        });

        modelBuilder.Entity("VaghtNegar.Domain.Entities.Patient", entity =>
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
