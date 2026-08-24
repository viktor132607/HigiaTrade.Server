using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HygiaTrade.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260824210000_AddProductNewStatuses")]
    public partial class AddProductNewStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductNewStatuses",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 14),
                    ActiveUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductNewStatuses", x => x.ProductId);
                    table.ForeignKey(
                        name: "FK_ProductNewStatuses_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductNewStatuses_ActiveUntilUtc",
                table: "ProductNewStatuses",
                column: "ActiveUntilUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProductNewStatuses");
        }
    }
}
