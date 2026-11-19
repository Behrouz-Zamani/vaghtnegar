using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VaghtNegar.Infrastructure.Persistence;

#nullable disable

namespace VaghtNegar.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VaghtNegarDbContext))]
[Migration("202611190001_AddDoctorIdentityUserId")]
public sealed class AddDoctorIdentityUserId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IdentityUserId",
            table: "Doctors",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Doctors_IdentityUserId",
            table: "Doctors",
            column: "IdentityUserId",
            unique: true,
            filter: "[IdentityUserId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Doctors_IdentityUserId", table: "Doctors");
        migrationBuilder.DropColumn(name: "IdentityUserId", table: "Doctors");
    }
}