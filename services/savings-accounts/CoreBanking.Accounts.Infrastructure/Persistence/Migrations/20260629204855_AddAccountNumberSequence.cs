using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreBanking.Accounts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "SAVINGS_ACCOUNT_NO_SEQ",
                schema: "SAVINGS");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "SAVINGS_ACCOUNT_NO_SEQ",
                schema: "SAVINGS");
        }
    }
}
