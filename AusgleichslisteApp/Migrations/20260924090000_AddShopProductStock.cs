using AusgleichslisteApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AusgleichslisteApp.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AusgleichslisteDbContext))]
    [Migration("20260924090000_AddShopProductStock")]
    public partial class AddShopProductStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "ShopProducts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ShopProducts_StockQuantity",
                table: "ShopProducts",
                column: "StockQuantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShopProducts_StockQuantity",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "ShopProducts");
        }
    }
}
