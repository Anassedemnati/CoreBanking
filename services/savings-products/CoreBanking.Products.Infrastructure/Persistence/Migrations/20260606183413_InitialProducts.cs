using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreBanking.Products.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "PRODUCTS");

            migrationBuilder.CreateTable(
                name: "OUTBOX_MESSAGES",
                schema: "PRODUCTS",
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
                name: "SAVINGS_PRODUCTS",
                schema: "PRODUCTS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    NAME = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    SHORTNAME = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    CURRENCYCODE = table.Column<string>(type: "NVARCHAR2(3)", maxLength: 3, nullable: false),
                    CURRENCYDECIMALPLACES = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    NOMINALANNUALRATE = table.Column<decimal>(type: "NUMBER(18,6)", nullable: false),
                    COMPOUNDINGPERIOD = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    POSTINGPERIOD = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CALCULATIONTYPE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DAYSINYEARTYPE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    STATUSENUM = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CREATEDONUTC = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    CREATEDBY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    LASTMODIFIEDONUTC = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    LASTMODIFIEDBY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    VERSION = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SAVINGS_PRODUCTS", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OUTBOX_MESSAGES",
                schema: "PRODUCTS");

            migrationBuilder.DropTable(
                name: "SAVINGS_PRODUCTS",
                schema: "PRODUCTS");
        }
    }
}
