using System;
using AusgleichslisteApp.Data;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AusgleichslisteApp.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AusgleichslisteDbContext))]
    [Migration("20260921090000_AddShopProducts")]
    public partial class AddShopProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceiverUserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ImageData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ImageContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ImageFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopProducts_Users_ReceiverUserId",
                        column: x => x.ReceiverUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopProducts_Barcode",
                table: "ShopProducts",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_ShopProducts_IsActive",
                table: "ShopProducts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ShopProducts_Name",
                table: "ShopProducts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ShopProducts_ReceiverUserId",
                table: "ShopProducts",
                column: "ReceiverUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopProducts");
        }
    }
}
