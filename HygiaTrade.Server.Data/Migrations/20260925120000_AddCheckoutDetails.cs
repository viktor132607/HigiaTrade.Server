using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace HygiaTrade.Data.Migrations;
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260925120000_AddCheckoutDetails")]
public class AddCheckoutDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "DeliveryRegion", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "PaymentMethod", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "DeliveryMethod", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "InvoiceCompanyName", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "InvoiceCompanyId", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "InvoiceVatId", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "InvoiceAddress", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "InvoiceRequested", table: "Orders", type: "boolean", nullable: false, defaultValue: false);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DeliveryRegion", table: "Orders");
        migrationBuilder.DropColumn(name: "PaymentMethod", table: "Orders");
        migrationBuilder.DropColumn(name: "DeliveryMethod", table: "Orders");
        migrationBuilder.DropColumn(name: "InvoiceCompanyName", table: "Orders");
        migrationBuilder.DropColumn(name: "InvoiceCompanyId", table: "Orders");
        migrationBuilder.DropColumn(name: "InvoiceVatId", table: "Orders");
        migrationBuilder.DropColumn(name: "InvoiceAddress", table: "Orders");
        migrationBuilder.DropColumn(name: "InvoiceRequested", table: "Orders");
    }
}
