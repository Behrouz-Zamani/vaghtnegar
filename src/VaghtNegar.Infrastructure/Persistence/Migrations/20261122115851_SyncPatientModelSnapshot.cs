using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaghtNegar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPatientModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty.
            // Patient schema changes are already applied by
            // 20261119184535_SyncPatientIdentityAndStatus.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. This migration only reconciles EF Core's model snapshot.
        }
    }
}
