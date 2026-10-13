using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HygiaTrade.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261013123000_AddOrderPackagingOption")]
public sealed class AddOrderPackagingOption : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<Guid>(
            name: "PackagingOptionId",
            table: "OrderItems",
            type: "uuid",
            nullable: true);
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "PackagingOptionId", table: "OrderItems");
}
