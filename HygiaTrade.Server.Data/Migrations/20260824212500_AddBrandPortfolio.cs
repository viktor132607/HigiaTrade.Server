using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HygiaTrade.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260824212500_AddBrandPortfolio")]
    public partial class AddBrandPortfolio : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ThumbnailImageUrl = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "Brands" ("Id", "CreatedOn", "ModifiedOn", "IsDeleted", "Name", "ThumbnailImageUrl", "Description")
                SELECT gen_random_uuid(), NOW(), NOW(), FALSE, brands."Brand", NULL, NULL
                FROM (
                    SELECT DISTINCT TRIM("Brand") AS "Brand"
                    FROM "Products"
                    WHERE "Brand" IS NOT NULL AND TRIM("Brand") <> ''
                ) brands
                WHERE NOT EXISTS (
                    SELECT 1 FROM "Brands" existing WHERE existing."Name" = brands."Brand"
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Brands");
        }
    }
}
