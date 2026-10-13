using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HygiaTrade.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261013121500_AddProductPackagingOptions")]
public sealed class AddProductPackagingOptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProductPackagingOptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                Label = table.Column<string>(type: "text", nullable: false),
                UnitsPerPackage = table.Column<int>(type: "integer", nullable: false),
                PriceInclVat = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                StockQuantity = table.Column<long>(type: "bigint", nullable: false),
                IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProductPackagingOptions", x => x.Id);
                table.ForeignKey("FK_ProductPackagingOptions_Products_ProductId",
                    x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "IX_ProductPackagingOptions_ProductId_SortOrder",
            table: "ProductPackagingOptions",
            columns: new[] { "ProductId", "SortOrder" });

        // Do not invent sellable inventory: non-default options start with zero stock.
        migrationBuilder.Sql("""
            INSERT INTO "ProductPackagingOptions"
                ("Id", "CreatedOn", "ModifiedOn", "IsDeleted", "ProductId",
                 "Label", "UnitsPerPackage", "PriceInclVat", "StockQuantity",
                 "IsDefault", "IsActive", "SortOrder")
            SELECT gen_random_uuid(), NOW(), NOW(), FALSE, p."Id",
                CASE WHEN p."Title" ILIKE '%Sano Oxygen Baby Gel%' THEN
                    CASE sizes.ordinal WHEN 0 THEN '1 л' WHEN 1 THEN '3 л'
                        WHEN 2 THEN 'Кашон 12 × 1 л' ELSE 'Кашон 20 × 3 л' END
                ELSE
                    CASE sizes.ordinal WHEN 0 THEN 'Единична опаковка'
                        WHEN 1 THEN 'Опаковка 3 бр.'
                        WHEN 2 THEN 'Кашон 12 бр.' ELSE 'Кашон 20 бр.' END
                END,
                sizes.units,
                ROUND(p."RegularPrice" * sizes.units, 2),
                0,
                sizes.ordinal = 0,
                TRUE,
                sizes.ordinal
            FROM "Products" p
            CROSS JOIN (VALUES (0,1),(1,3),(2,12),(3,60)) AS sizes(ordinal, units)
            WHERE NOT p."IsDeleted"
              AND NOT EXISTS (SELECT 1 FROM "ProductPackagingOptions" o
                    WHERE o."ProductId" = p."Id");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProductPackagingOptions");
    }
}
