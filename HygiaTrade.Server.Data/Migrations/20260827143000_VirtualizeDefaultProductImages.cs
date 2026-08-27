using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HygiaTrade.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260827143000_VirtualizeDefaultProductImages")]
    public partial class VirtualizeDefaultProductImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Products" p
                SET "MainImageUrl" = '',
                    "ModifiedOn" = NOW()
                WHERE TRIM(p."MainImageUrl") <> ''
                  AND (
                      LOWER(SPLIT_PART(SPLIT_PART(TRIM(p."MainImageUrl"), '?', 1), '#', 1)) LIKE '%/higiqlogo.png'
                      OR LOWER(SPLIT_PART(SPLIT_PART(TRIM(p."MainImageUrl"), '?', 1), '#', 1)) = 'higiqlogo.png'
                      OR EXISTS (
                          SELECT 1
                          FROM "Brands" b
                          WHERE NOT b."IsDeleted"
                            AND p."Brand" IS NOT NULL
                            AND LOWER(TRIM(p."Brand")) = LOWER(TRIM(b."Name"))
                            AND b."ThumbnailImageUrl" IS NOT NULL
                            AND TRIM(b."ThumbnailImageUrl") <> ''
                            AND TRIM(p."MainImageUrl") = TRIM(b."ThumbnailImageUrl")
                      )
                  );

                DELETE FROM "Images" i
                USING "Products" p
                WHERE i."ProductId" = p."Id"
                  AND (
                      LOWER(SPLIT_PART(SPLIT_PART(TRIM(i."Uri"), '?', 1), '#', 1)) LIKE '%/higiqlogo.png'
                      OR LOWER(SPLIT_PART(SPLIT_PART(TRIM(i."Uri"), '?', 1), '#', 1)) = 'higiqlogo.png'
                      OR EXISTS (
                          SELECT 1
                          FROM "Brands" b
                          WHERE NOT b."IsDeleted"
                            AND p."Brand" IS NOT NULL
                            AND LOWER(TRIM(p."Brand")) = LOWER(TRIM(b."Name"))
                            AND b."ThumbnailImageUrl" IS NOT NULL
                            AND TRIM(b."ThumbnailImageUrl") <> ''
                            AND TRIM(i."Uri") = TRIM(b."ThumbnailImageUrl")
                      )
                  );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The deleted duplicate fallback references are intentionally not recreated.
        }
    }
}
