using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace HygiaTrade.Data.Migrations;
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260925170000_AddStripeCheckout")]
public class AddStripeCheckout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "PaymentStatus", table: "Orders", type: "text", nullable: false, defaultValue: "Unpaid");
        migrationBuilder.AddColumn<Guid>(name: "StripeCheckoutAttemptId", table: "Orders", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<string>(name: "StripeCheckoutFingerprint", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "StripeSessionId", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "StripeCheckoutUrl", table: "Orders", type: "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "StripeExpiresAt", table: "Orders", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<bool>(name: "StripeLiveMode", table: "Orders", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.CreateIndex(name: "IX_Orders_StripeSessionId", table: "Orders", column: "StripeSessionId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Orders_StripeCheckoutAttemptId", table: "Orders", column: "StripeCheckoutAttemptId", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Orders_StripeCheckoutAttemptId", table: "Orders");
        migrationBuilder.DropIndex(name: "IX_Orders_StripeSessionId", table: "Orders");
        migrationBuilder.DropColumn(name: "PaymentStatus", table: "Orders");
        migrationBuilder.DropColumn(name: "StripeCheckoutAttemptId", table: "Orders");
        migrationBuilder.DropColumn(name: "StripeCheckoutFingerprint", table: "Orders");
        migrationBuilder.DropColumn(name: "StripeSessionId", table: "Orders");
        migrationBuilder.DropColumn(name: "StripeCheckoutUrl", table: "Orders");
        migrationBuilder.DropColumn(name: "StripeExpiresAt", table: "Orders");
        migrationBuilder.DropColumn(name: "StripeLiveMode", table: "Orders");
    }
}
