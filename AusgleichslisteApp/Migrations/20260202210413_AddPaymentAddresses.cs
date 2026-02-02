using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AusgleichslisteApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayPalAddress",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeroAddress",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayPalAddress",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WeroAddress",
                table: "Users");
        }
    }
}
