using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AjandaAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivitySeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "series_id",
                table: "activities",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "activity_series",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    days_of_week = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_activity_series", x => x.id);
                    table.ForeignKey(
                        name: "fk_activity_series_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activities_series_id",
                table: "activities",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "ix_activity_series_user_id",
                table: "activity_series",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_activities_activity_series_series_id",
                table: "activities",
                column: "series_id",
                principalTable: "activity_series",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_activities_activity_series_series_id",
                table: "activities");

            migrationBuilder.DropTable(
                name: "activity_series");

            migrationBuilder.DropIndex(
                name: "ix_activities_series_id",
                table: "activities");

            migrationBuilder.DropColumn(
                name: "series_id",
                table: "activities");
        }
    }
}
