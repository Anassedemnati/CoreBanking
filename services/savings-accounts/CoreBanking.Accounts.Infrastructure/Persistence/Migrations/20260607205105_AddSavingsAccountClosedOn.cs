using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreBanking.Accounts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsAccountClosedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CLOSEDON",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS",
                type: "NVARCHAR2(10)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CLOSEDON",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS");
        }
    }
}
