using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AjandaAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 1,
                column: "sort_order",
                value: 1);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 2,
                column: "sort_order",
                value: 2);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 3,
                column: "sort_order",
                value: 3);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 4,
                column: "sort_order",
                value: 4);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 5,
                column: "sort_order",
                value: 5);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 6,
                column: "sort_order",
                value: 6);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 7,
                column: "sort_order",
                value: 7);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 8,
                column: "sort_order",
                value: 8);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 9,
                column: "sort_order",
                value: 9);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: 10,
                column: "sort_order",
                value: 10);

            // Seed dışı mevcut kayıtlar dahil tüm kategorileri Id sırasıyla 1..N numaralar.
            migrationBuilder.Sql(@"
                UPDATE categories AS c
                SET sort_order = o.rn
                FROM (SELECT id, ROW_NUMBER() OVER (ORDER BY id) AS rn FROM categories) AS o
                WHERE c.id = o.id;");

            migrationBuilder.CreateIndex(
                name: "ix_categories_sort_order",
                table: "categories",
                column: "sort_order");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_categories_sort_order",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "categories");
        }
    }
}
