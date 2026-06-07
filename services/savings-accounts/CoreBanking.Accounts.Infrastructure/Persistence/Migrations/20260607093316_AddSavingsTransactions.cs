using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreBanking.Accounts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ACCOUNTBALANCE",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS",
                type: "NUMBER(19,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "COMPOUNDINGENUM",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DAYSINYEARENUM",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "INTERESTPOSTEDTILLDATE",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS",
                type: "NVARCHAR2(10)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "POSTINGPERIODENUM",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SAVINGS_ACCOUNT_TRANSACTIONS",
                schema: "SAVINGS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ACCOUNTID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    SEQUENCENO = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    TYPEENUM = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    TRANSACTIONDATE = table.Column<string>(type: "NVARCHAR2(10)", nullable: false),
                    AMOUNT = table.Column<decimal>(type: "NUMBER(19,6)", nullable: false),
                    RUNNINGBALANCE = table.Column<decimal>(type: "NUMBER(19,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAVINGS_ACCOUNT_TRANSACTIONS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SAVINGS_ACCOUNT_TRANSACTIONS_SAVINGS_ACCOUNTS_ACCOUNTID",
                        column: x => x.ACCOUNTID,
                        principalSchema: "SAVINGS",
                        principalTable: "SAVINGS_ACCOUNTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SAVINGS_ACCOUNT_TRANSACTIONS_ACCOUNTID_TRANSACTIONDATE",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNT_TRANSACTIONS",
                columns: new[] { "ACCOUNTID", "TRANSACTIONDATE" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SAVINGS_ACCOUNT_TRANSACTIONS",
                schema: "SAVINGS");

            migrationBuilder.DropColumn(
                name: "ACCOUNTBALANCE",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS");

            migrationBuilder.DropColumn(
                name: "COMPOUNDINGENUM",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS");

            migrationBuilder.DropColumn(
                name: "DAYSINYEARENUM",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS");

            migrationBuilder.DropColumn(
                name: "INTERESTPOSTEDTILLDATE",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS");

            migrationBuilder.DropColumn(
                name: "POSTINGPERIODENUM",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS");
        }
    }
}
