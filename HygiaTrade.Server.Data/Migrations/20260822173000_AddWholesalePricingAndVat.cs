using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using HygiaTrade.Data;

#nullable disable

namespace HygiaTrade.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260822173000_AddWholesalePricingAndVat")]
    public partial class AddWholesalePricingAndVat : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "Products",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 20m);

            migrationBuilder.AddColumn<decimal>(
                name: "WholesalePrice",
                table: "Products",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "WholesaleMinQuantity",
                table: "Products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "SinglePriceExclVat",
                table: "OrderItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPriceExclVat",
                table: "OrderItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "OrderItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "OrderItems",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 20m);

            migrationBuilder.AddColumn<int>(
                name: "PricingTier",
                table: "OrderItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderSubtotalExclVat",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderVatAmount",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE "OrderItems" AS oi
                SET
                    "VatRate" = p."VatRate",
                    "SinglePriceExclVat" = ROUND(oi."SinglePrice" / (1 + p."VatRate" / 100), 2),
                    "TotalPriceExclVat" = ROUND(oi."TotalPrice" / (1 + p."VatRate" / 100), 2),
                    "VatAmount" = ROUND(oi."TotalPrice" - (oi."TotalPrice" / (1 + p."VatRate" / 100)), 2),
                    "PricingTier" = 0
                FROM "Products" AS p
                WHERE oi."ProductId" = p."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Orders" AS o
                SET
                    "OrderSubtotalExclVat" = COALESCE(t."NetTotal", 0),
                    "OrderVatAmount" = COALESCE(t."VatTotal", 0),
                    "OrderTotalPrice" = COALESCE(t."GrossTotal", 0)
                FROM (
                    SELECT
                        "OrderId",
                        ROUND(SUM("TotalPriceExclVat"), 2) AS "NetTotal",
                        ROUND(SUM("VatAmount"), 2) AS "VatTotal",
                        ROUND(SUM("TotalPrice"), 2) AS "GrossTotal"
                    FROM "OrderItems"
                    WHERE "IsDeleted" = false
                    GROUP BY "OrderId"
                ) AS t
                WHERE o."Id" = t."OrderId";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "VatRate", table: "Products");
            migrationBuilder.DropColumn(name: "WholesalePrice", table: "Products");
            migrationBuilder.DropColumn(name: "WholesaleMinQuantity", table: "Products");

            migrationBuilder.DropColumn(name: "SinglePriceExclVat", table: "OrderItems");
            migrationBuilder.DropColumn(name: "TotalPriceExclVat", table: "OrderItems");
            migrationBuilder.DropColumn(name: "VatAmount", table: "OrderItems");
            migrationBuilder.DropColumn(name: "VatRate", table: "OrderItems");
            migrationBuilder.DropColumn(name: "PricingTier", table: "OrderItems");

            migrationBuilder.DropColumn(name: "OrderSubtotalExclVat", table: "Orders");
            migrationBuilder.DropColumn(name: "OrderVatAmount", table: "Orders");
        }
    }
}
