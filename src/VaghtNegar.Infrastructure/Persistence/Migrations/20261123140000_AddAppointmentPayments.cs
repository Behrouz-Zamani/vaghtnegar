using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VaghtNegar.Infrastructure.Persistence;

#nullable disable

namespace VaghtNegar.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VaghtNegarDbContext))]
[Migration("20261123140000_AddAppointmentPayments")]
public partial class AddAppointmentPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppointmentPayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AmountToman = table.Column<long>(type: "bigint", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ReceiptReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                ReceiptContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ReceiptImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ReceiptSubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                ReviewedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppointmentPayments", x => x.Id);
                table.ForeignKey("FK_AppointmentPayments_Appointments_AppointmentId", x => x.AppointmentId,
                    "Appointments", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_AppointmentPayments_AppointmentId", "AppointmentPayments", "AppointmentId", unique: true);
        migrationBuilder.CreateIndex("IX_AppointmentPayments_ExpiresAtUtc", "AppointmentPayments", "ExpiresAtUtc");
        migrationBuilder.CreateIndex("IX_AppointmentPayments_PatientId_CreatedAtUtc", "AppointmentPayments", new[] { "PatientId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex("IX_AppointmentPayments_Status_ExpiresAtUtc", "AppointmentPayments", new[] { "Status", "ExpiresAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AppointmentPayments");
    }
}
