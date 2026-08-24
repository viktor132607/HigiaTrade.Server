using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HygiaTrade.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260824235000_RemovePlaceholderOperationalData")]
    public partial class RemovePlaceholderOperationalData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "WishlistItems"
                WHERE "UserId" IN (
                    SELECT "Id" FROM "Users"
                    WHERE LOWER("Email") IN (
                        'martin.georgiev@hygiatrade.bg',
                        'maria.dimitrova@hygiatrade.bg',
                        'ivan.petrov@hygiatrade.bg',
                        'petya.stoyanova@hygiatrade.bg',
                        'georgi.kolev@hygiatrade.bg',
                        'nikol.todorov@hygiatrade.bg',
                        'borislava.ilieva@hygiatrade.bg',
                        'daniel.nikolov@hygiatrade.bg',
                        'teodora.marinova@hygiatrade.bg',
                        'stela.atanasova@hygiatrade.bg'
                    )
                );

                DELETE FROM "Reviews"
                WHERE "UserId" IN (
                    SELECT "Id" FROM "Users"
                    WHERE LOWER("Email") IN (
                        'martin.georgiev@hygiatrade.bg',
                        'maria.dimitrova@hygiatrade.bg',
                        'ivan.petrov@hygiatrade.bg',
                        'petya.stoyanova@hygiatrade.bg',
                        'georgi.kolev@hygiatrade.bg',
                        'nikol.todorov@hygiatrade.bg',
                        'borislava.ilieva@hygiatrade.bg',
                        'daniel.nikolov@hygiatrade.bg',
                        'teodora.marinova@hygiatrade.bg',
                        'stela.atanasova@hygiatrade.bg'
                    )
                );

                DELETE FROM "OrderItems"
                WHERE "OrderId" IN (
                    SELECT o."Id"
                    FROM "Orders" o
                    JOIN "Users" u ON u."Id" = o."UserId"
                    WHERE LOWER(u."Email") IN (
                        'martin.georgiev@hygiatrade.bg',
                        'maria.dimitrova@hygiatrade.bg',
                        'ivan.petrov@hygiatrade.bg',
                        'petya.stoyanova@hygiatrade.bg',
                        'georgi.kolev@hygiatrade.bg',
                        'nikol.todorov@hygiatrade.bg',
                        'borislava.ilieva@hygiatrade.bg',
                        'daniel.nikolov@hygiatrade.bg',
                        'teodora.marinova@hygiatrade.bg',
                        'stela.atanasova@hygiatrade.bg'
                    )
                );

                DELETE FROM "Orders"
                WHERE "UserId" IN (
                    SELECT "Id" FROM "Users"
                    WHERE LOWER("Email") IN (
                        'martin.georgiev@hygiatrade.bg',
                        'maria.dimitrova@hygiatrade.bg',
                        'ivan.petrov@hygiatrade.bg',
                        'petya.stoyanova@hygiatrade.bg',
                        'georgi.kolev@hygiatrade.bg',
                        'nikol.todorov@hygiatrade.bg',
                        'borislava.ilieva@hygiatrade.bg',
                        'daniel.nikolov@hygiatrade.bg',
                        'teodora.marinova@hygiatrade.bg',
                        'stela.atanasova@hygiatrade.bg'
                    )
                );

                DELETE FROM "Users"
                WHERE LOWER("Email") IN (
                    'martin.georgiev@hygiatrade.bg',
                    'maria.dimitrova@hygiatrade.bg',
                    'ivan.petrov@hygiatrade.bg',
                    'petya.stoyanova@hygiatrade.bg',
                    'georgi.kolev@hygiatrade.bg',
                    'nikol.todorov@hygiatrade.bg',
                    'borislava.ilieva@hygiatrade.bg',
                    'daniel.nikolov@hygiatrade.bg',
                    'teodora.marinova@hygiatrade.bg',
                    'stela.atanasova@hygiatrade.bg'
                );

                -- All existing promotional values were seeded/demo values. Promotions start clean
                -- and are henceforth stored only when an administrator explicitly configures them.
                UPDATE "Products"
                SET "DiscountPercentage" = 0,
                    "DiscountedPrice" = 0,
                    "ModifiedOn" = NOW();

                -- Remove seeded stock. Products that have a real receipt ledger retain their current
                -- quantity, because InventoryController and checkout already maintain that value.
                UPDATE "Products" p
                SET "Quantity" = 0,
                    "ModifiedOn" = NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM "StockEntries" s WHERE s."ProductId" = p."Id"
                );

                -- Ratings are derived exclusively from remaining real reviews.
                UPDATE "Products" p
                SET "Rating" = COALESCE((
                    SELECT ROUND(AVG(r."Rating")::numeric, 2)::double precision
                    FROM "Reviews" r
                    WHERE r."ProductId" = p."Id" AND r."IsDeleted" = FALSE
                ), 0),
                "ModifiedOn" = NOW();
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data cleanup: fabricated operational values must not be restored.
        }
    }
}
