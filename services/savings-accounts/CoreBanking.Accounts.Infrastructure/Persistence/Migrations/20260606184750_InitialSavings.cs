using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreBanking.Accounts.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSavings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SAVINGS");

            migrationBuilder.CreateTable(
                name: "CLIENT_REF",
                schema: "SAVINGS",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    DISPLAYNAME = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    ISACTIVE = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    EVENTVERSION = table.Column<long>(type: "NUMBER(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLIENT_REF", x => x.ClientId);
                });

            migrationBuilder.CreateTable(
                name: "INBOX_MESSAGES",
                schema: "SAVINGS",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Type = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ReceivedOnUtc = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INBOX_MESSAGES", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "OUTBOX_MESSAGES",
                schema: "SAVINGS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Type = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Topic = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    AggregateKey = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Content = table.Column<string>(type: "CLOB", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    Error = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OUTBOX_MESSAGES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCT_REF",
                schema: "SAVINGS",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    CURRENCYCODE = table.Column<string>(type: "NVARCHAR2(3)", maxLength: 3, nullable: false),
                    CURRENCYDECIMALPLACES = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DEFAULTRATE = table.Column<decimal>(type: "NUMBER(18,6)", nullable: false),
                    EVENTVERSION = table.Column<long>(type: "NUMBER(19)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCT_REF", x => x.ProductId);
                });

            migrationBuilder.CreateTable(
                name: "SAVINGS_ACCOUNTS",
                schema: "SAVINGS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    ACCOUNTNO = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    CLIENTID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    PRODUCTID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    STATUSENUM = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CURRENCYCODE = table.Column<string>(type: "NVARCHAR2(3)", maxLength: 3, nullable: false),
                    CURRENCYDECIMALPLACES = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    NOMINALANNUALRATE = table.Column<decimal>(type: "NUMBER(18,6)", nullable: false),
                    SUBMITTEDON = table.Column<string>(type: "NVARCHAR2(10)", nullable: false),
                    APPROVEDON = table.Column<string>(type: "NVARCHAR2(10)", nullable: true),
                    ACTIVATEDON = table.Column<string>(type: "NVARCHAR2(10)", nullable: true),
                    REJECTEDON = table.Column<string>(type: "NVARCHAR2(10)", nullable: true),
                    WITHDRAWNON = table.Column<string>(type: "NVARCHAR2(10)", nullable: true),
                    CREATEDONUTC = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    CREATEDBY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    LASTMODIFIEDONUTC = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    LASTMODIFIEDBY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    VERSION = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAVINGS_ACCOUNTS", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SAVINGS_ACCOUNTS_ACCOUNTNO",
                schema: "SAVINGS",
                table: "SAVINGS_ACCOUNTS",
                column: "ACCOUNTNO",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CLIENT_REF",
                schema: "SAVINGS");

            migrationBuilder.DropTable(
                name: "INBOX_MESSAGES",
                schema: "SAVINGS");

            migrationBuilder.DropTable(
                name: "OUTBOX_MESSAGES",
                schema: "SAVINGS");

            migrationBuilder.DropTable(
                name: "PRODUCT_REF",
                schema: "SAVINGS");

            migrationBuilder.DropTable(
                name: "SAVINGS_ACCOUNTS",
                schema: "SAVINGS");
        }
    }
}
