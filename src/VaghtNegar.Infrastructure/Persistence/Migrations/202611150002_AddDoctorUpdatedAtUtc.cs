using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VaghtNegar.Infrastructure.Persistence;

#nullable disable

namespace VaghtNegar.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VaghtNegarDbContext))]
[Migration("202611150002_AddDoctorUpdatedAtUtc")]
public sealed class AddDoctorUpdatedAtUtc : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "UpdatedAtUtc",
            table: "Doctors",
            type: "datetimeoffset",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "UpdatedAtUtc", table: "Doctors");
    }
}