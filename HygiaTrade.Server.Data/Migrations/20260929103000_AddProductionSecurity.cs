using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HygiaTrade.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260929103000_AddProductionSecurity")]
public sealed class AddProductionSecurity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "PasswordResetTokens" (
                "TokenHash" bytea PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "ExpiresAtUtc" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_PasswordResetTokens_ExpiresAtUtc"
                ON "PasswordResetTokens" ("ExpiresAtUtc");

            ALTER TABLE "WishlistItems" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "WishlistItems" FORCE ROW LEVEL SECURITY;

            DROP POLICY IF EXISTS "WishlistItems_Select" ON "WishlistItems";
            DROP POLICY IF EXISTS "WishlistItems_Insert" ON "WishlistItems";
            DROP POLICY IF EXISTS "WishlistItems_Update" ON "WishlistItems";
            DROP POLICY IF EXISTS "WishlistItems_Delete" ON "WishlistItems";

            CREATE POLICY "WishlistItems_Select" ON "WishlistItems"
            FOR SELECT USING (
                current_setting('app.current_is_system', true) = 'true'
                OR current_setting('app.current_is_admin', true) = 'true'
                OR "UserId"::text = current_setting('app.current_user_id', true)
            );
            CREATE POLICY "WishlistItems_Insert" ON "WishlistItems"
            FOR INSERT WITH CHECK (
                current_setting('app.current_is_system', true) = 'true'
                OR current_setting('app.current_is_admin', true) = 'true'
                OR "UserId"::text = current_setting('app.current_user_id', true)
            );
            CREATE POLICY "WishlistItems_Update" ON "WishlistItems"
            FOR UPDATE USING (
                current_setting('app.current_is_system', true) = 'true'
                OR current_setting('app.current_is_admin', true) = 'true'
                OR "UserId"::text = current_setting('app.current_user_id', true)
            ) WITH CHECK (
                current_setting('app.current_is_system', true) = 'true'
                OR current_setting('app.current_is_admin', true) = 'true'
                OR "UserId"::text = current_setting('app.current_user_id', true)
            );
            CREATE POLICY "WishlistItems_Delete" ON "WishlistItems"
            FOR DELETE USING (
                current_setting('app.current_is_system', true) = 'true'
                OR current_setting('app.current_is_admin', true) = 'true'
                OR "UserId"::text = current_setting('app.current_user_id', true)
            );

            ALTER TABLE "Reviews" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "Reviews" FORCE ROW LEVEL SECURITY;

            DROP POLICY IF EXISTS "Reviews_Select" ON "Reviews";
            DROP POLICY IF EXISTS "Reviews_Insert" ON "Reviews";
            DROP POLICY IF EXISTS "Reviews_Update" ON "Reviews";
            DROP POLICY IF EXISTS "Reviews_Delete" ON "Reviews";

            CREATE POLICY "Reviews_Select" ON "Reviews"
            FOR SELECT USING (true);
            CREATE POLICY "Reviews_Insert" ON "Reviews"
            FOR INSERT WITH CHECK (
                current_setting('app.current_is_system', true) = 'true'
                OR current_setting('app.current_is_admin', true) = 'true'
                OR "UserId"::text = current_setting('app.current_user_id', true)
            );
            CREATE POLICY "Reviews_Update" ON "Reviews"
            FOR UPDATE USING (
                current_setting('app.current_is_system', true) = 'true'
                OR current_setting('app.current_is_admin', true) = 'true'
                OR "UserId"::text = current_setting('app.current_user_id', true)
            ) WITH CHECK (
                current_setting('app.current_is_system', true) = 'true'
                OR current_setting('app.current_is_admin', true) = 'true'
                OR "UserId"::text = current_setting('app.current_user_id', true)
            );
            CREATE POLICY "Reviews_Delete" ON "Reviews"
            FOR DELETE USING (
                current_setting('app.current_is_system', true) = 'true'
                OR current_setting('app.current_is_admin', true) = 'true'
                OR "UserId"::text = current_setting('app.current_user_id', true)
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "WishlistItems" DISABLE ROW LEVEL SECURITY;
            ALTER TABLE "Reviews" DISABLE ROW LEVEL SECURITY;
            DROP POLICY IF EXISTS "WishlistItems_Select" ON "WishlistItems";
            DROP POLICY IF EXISTS "WishlistItems_Insert" ON "WishlistItems";
            DROP POLICY IF EXISTS "WishlistItems_Update" ON "WishlistItems";
            DROP POLICY IF EXISTS "WishlistItems_Delete" ON "WishlistItems";
            DROP POLICY IF EXISTS "Reviews_Select" ON "Reviews";
            DROP POLICY IF EXISTS "Reviews_Insert" ON "Reviews";
            DROP POLICY IF EXISTS "Reviews_Update" ON "Reviews";
            DROP POLICY IF EXISTS "Reviews_Delete" ON "Reviews";
            DROP TABLE IF EXISTS "PasswordResetTokens";
            """);
    }
}
