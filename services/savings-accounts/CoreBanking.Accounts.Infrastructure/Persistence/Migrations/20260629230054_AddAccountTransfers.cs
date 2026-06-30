using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreBanking.Accounts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ACCOUNT_TRANSFERS",
                schema: "SAVINGS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    SOURCEACCOUNTID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    DESTINATIONACCOUNTID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    SOURCETRANSACTIONID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    DESTINATIONTRANSACTIONID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    AMOUNT = table.Column<decimal>(type: "NUMBER(19,6)", nullable: false),
                    CURRENCYCODE = table.Column<string>(type: "NVARCHAR2(3)", maxLength: 3, nullable: false),
                    TRANSFERDATE = table.Column<string>(type: "NVARCHAR2(10)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    CLIENTTRANSFERREFERENCE = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    CREATEDONUTC = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    CREATEDBY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    LASTMODIFIEDONUTC = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    LASTMODIFIEDBY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    VERSION = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCOUNT_TRANSFERS", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_TRANSFERS_CLIENTTRANSFERREFERENCE",
                schema: "SAVINGS",
                table: "ACCOUNT_TRANSFERS",
                column: "CLIENTTRANSFERREFERENCE",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_TRANSFERS_DESTINATIONTRANSACTIONID",
                schema: "SAVINGS",
                table: "ACCOUNT_TRANSFERS",
                column: "DESTINATIONTRANSACTIONID");

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_TRANSFERS_SOURCETRANSACTIONID",
                schema: "SAVINGS",
                table: "ACCOUNT_TRANSFERS",
                column: "SOURCETRANSACTIONID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ACCOUNT_TRANSFERS",
                schema: "SAVINGS");
        }
    }
}
