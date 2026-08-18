using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HygiaTrade.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCurrencyToEur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Products"
                SET
                    "RegularPrice" = ROUND("RegularPrice" / 1.95583, 2),
                    "DiscountedPrice" = CASE
                        WHEN "DiscountedPrice" = 0 THEN 0
                        ELSE ROUND("DiscountedPrice" / 1.95583, 2)
                    END;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "OrderItems"
                SET
                    "SinglePrice" = ROUND("SinglePrice" / 1.95583, 2),
                    "TotalPrice" = ROUND("TotalPrice" / 1.95583, 2);
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "OrderTotalPrice",
                table: "Orders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "OrderItems",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.Sql(
                """
                UPDATE "Orders" AS "Order"
                SET "OrderTotalPrice" = COALESCE("OrderTotals"."OrderTotalPrice", 0)
                FROM (
                    SELECT "OrderId", ROUND(SUM("TotalPrice"), 2) AS "OrderTotalPrice"
                    FROM "OrderItems"
                    GROUP BY "OrderId"
                ) AS "OrderTotals"
                WHERE "Order"."Id" = "OrderTotals"."OrderId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Orders" AS "Order"
                SET "OrderTotalPrice" = 0
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "OrderItems" AS "OrderItem"
                    WHERE "OrderItem"."OrderId" = "Order"."Id"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Products"
                SET
                    "RegularPrice" = ROUND("RegularPrice" * 1.95583, 2),
                    "DiscountedPrice" = CASE
                        WHEN "DiscountedPrice" = 0 THEN 0
                        ELSE ROUND("DiscountedPrice" * 1.95583, 2)
                    END;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "OrderItems"
                SET
                    "SinglePrice" = ROUND("SinglePrice" * 1.95583, 2),
                    "TotalPrice" = ROUND("TotalPrice" * 1.95583, 2);
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "OrderTotalPrice",
                table: "Orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalPrice",
                table: "OrderItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.Sql(
                """
                UPDATE "Orders" AS "Order"
                SET "OrderTotalPrice" = COALESCE("OrderTotals"."OrderTotalPrice", 0)
                FROM (
                    SELECT "OrderId", ROUND(SUM("TotalPrice"), 2) AS "OrderTotalPrice"
                    FROM "OrderItems"
                    GROUP BY "OrderId"
                ) AS "OrderTotals"
                WHERE "Order"."Id" = "OrderTotals"."OrderId";
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Orders" AS "Order"
                SET "OrderTotalPrice" = 0
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "OrderItems" AS "OrderItem"
                    WHERE "OrderItem"."OrderId" = "Order"."Id"
                );
                """);
        }
    }
}
