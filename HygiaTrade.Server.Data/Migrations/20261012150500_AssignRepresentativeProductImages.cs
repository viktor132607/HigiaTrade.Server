using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HygiaTrade.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261012150500_AssignRepresentativeProductImages")]
public sealed class AssignRepresentativeProductImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsRepresentativeImage",
            table: "Products",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        // Reuse existing stored image URLs, without duplicating binary data.
        // Pick a different donor pseudo-randomly for each target (stable and
        // independently selected via its UUID); never change existing photos.
        migrationBuilder.Sql("""
            WITH assignments AS (
                SELECT DISTINCT ON (target."Id")
                    target."Id" AS target_id,
                    donor."MainImageUrl" AS image_url
                FROM "Products" AS target
                CROSS JOIN "Products" AS donor
                WHERE NOT target."IsDeleted"
                  AND NULLIF(BTRIM(target."MainImageUrl"), '') IS NULL
                  AND NOT donor."IsDeleted"
                  AND NULLIF(BTRIM(donor."MainImageUrl"), '') IS NOT NULL
                  AND donor."MainImageUrl" LIKE '%/api/Images/%'
                  AND EXISTS (
                      SELECT 1
                      FROM "StoredImages" AS stored
                      WHERE NOT stored."IsDeleted"
                        AND stored."Id"::text =
                            RIGHT(donor."MainImageUrl", 36)
                  )
                ORDER BY target."Id",
                    md5(target."Id"::text || ':' || donor."Id"::text)
            )
            UPDATE "Products" AS target
            SET "MainImageUrl" = assignments.image_url,
                "IsRepresentativeImage" = TRUE,
                "ModifiedOn" = NOW()
            FROM assignments
            WHERE target."Id" = assignments.target_id
              AND NOT target."IsDeleted"
              AND NULLIF(BTRIM(target."MainImageUrl"), '') IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert only images that are still flagged as representative.
        migrationBuilder.Sql("""
            UPDATE "Products"
            SET "MainImageUrl" = '',
                "ModifiedOn" = NOW()
            WHERE "IsRepresentativeImage"
              AND NOT "IsDeleted";
            """);

        migrationBuilder.DropColumn(
            name: "IsRepresentativeImage",
            table: "Products");
    }
}
