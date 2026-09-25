using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AjandaAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserEmailIndexCaseInsensitive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_email",
                table: "users");

            // Expression index EF model'inde yok; raw SQL ile yonetilir (bkz. UserConfiguration).
            migrationBuilder.Sql("CREATE UNIQUE INDEX ix_users_email_lower ON users (lower(email));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_users_email_lower;");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }
    }
}
