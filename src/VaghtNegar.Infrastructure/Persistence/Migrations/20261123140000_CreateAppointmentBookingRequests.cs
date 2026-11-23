using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VaghtNegar.Infrastructure.Persistence;

#nullable disable

namespace VaghtNegar.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VaghtNegarDbContext))]
[Migration("20261123140000_CreateAppointmentBookingRequests")]
public sealed class CreateAppointmentBookingRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppointmentBookingRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                IdentityUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                VisitFeeToman = table.Column<long>(type: "bigint", nullable: false),
                PatientNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                AccessTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                HoldExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ReceiptContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                ReceiptContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ReceiptFileName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                ReceiptSubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                AdminNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ReviewedByIdentityUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AppointmentBookingRequests", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_AppointmentBookingRequests_AccessTokenHash",
            table: "AppointmentBookingRequests",
            column: "AccessTokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_AppointmentBookingRequests_DoctorId_StartsAtUtc_Status",
            table: "AppointmentBookingRequests",
            columns: new[] { "DoctorId", "StartsAtUtc", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_AppointmentBookingRequests_PatientId_CreatedAtUtc",
            table: "AppointmentBookingRequests",
            columns: new[] { "PatientId", "CreatedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppointmentBookingRequests");
    }
}
