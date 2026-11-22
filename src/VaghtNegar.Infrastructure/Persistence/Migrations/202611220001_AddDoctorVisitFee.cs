using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VaghtNegar.Infrastructure.Persistence;

#nullable disable

namespace VaghtNegar.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VaghtNegarDbContext))]
[Migration("202611220001_AddDoctorVisitFee")]
public sealed class AddDoctorVisitFee : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "VisitFeeToman",
            table: "Doctors",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "VisitFeeToman", table: "Doctors");
    }
}