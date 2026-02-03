using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AusgleichslisteApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayerId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RecipientId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SuggestedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settlements_Users_PayerId",
                        column: x => x.PayerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Settlements_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_IsActive",
                table: "Settlements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_PayerId",
                table: "Settlements",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_RecipientId",
                table: "Settlements",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_SuggestedDate",
                table: "Settlements",
                column: "SuggestedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settlements");
        }
    }
}
