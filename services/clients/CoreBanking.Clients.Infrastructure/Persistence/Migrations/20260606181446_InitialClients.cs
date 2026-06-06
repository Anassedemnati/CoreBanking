using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreBanking.Clients.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "CLIENTS");

            migrationBuilder.CreateTable(
                name: "CLIENTS",
                schema: "CLIENTS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    DISPLAYNAME = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    EXTERNALID = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    STATUSENUM = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ACTIVATIONDATE = table.Column<string>(type: "NVARCHAR2(10)", nullable: true),
                    CREATEDONUTC = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    CREATEDBY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    LASTMODIFIEDONUTC = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    LASTMODIFIEDBY = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    VERSION = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CLIENTS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OUTBOX_MESSAGES",
                schema: "CLIENTS",
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

            migrationBuilder.CreateIndex(
                name: "IX_CLIENTS_EXTERNALID",
                schema: "CLIENTS",
                table: "CLIENTS",
                column: "EXTERNALID",
                unique: true,
                filter: "\"EXTERNALID\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CLIENTS",
                schema: "CLIENTS");

            migrationBuilder.DropTable(
                name: "OUTBOX_MESSAGES",
                schema: "CLIENTS");
        }
    }
}
