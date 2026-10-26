using AusgleichslisteApp.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AusgleichslisteApp.Migrations;

[DbContext(typeof(AusgleichslisteDbContext))]
[Migration("20261026090000_AddUserNfcId")]
public class AddUserNfcId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "NfcId", table: "Users", type: "TEXT", maxLength: 128, nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_Users_NfcId", table: "Users", column: "NfcId", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Users_NfcId", table: "Users");
        migrationBuilder.DropColumn(name: "NfcId", table: "Users");
    }
}
