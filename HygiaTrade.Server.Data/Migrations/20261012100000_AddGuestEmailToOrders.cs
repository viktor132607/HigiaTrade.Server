using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HygiaTrade.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261012100000_AddGuestEmailToOrders")]
public sealed class AddGuestEmailToOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "GuestEmail",
            table: "Orders",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "GuestEmail",
            table: "Orders");
    }
}
