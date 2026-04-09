using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WalkerFcb.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMealPlanAndSuggestionsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meal_plan_slots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slot_date = table.Column<DateOnly>(type: "date", nullable: false),
                    slot_type = table.Column<string>(type: "text", nullable: false),
                    recipe_id = table.Column<int>(type: "integer", nullable: true),
                    batch_multiplier = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    notes = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meal_plan_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_meal_plan_slots_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipe_suggestions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    suggested_by = table.Column<int>(type: "integer", nullable: false),
                    suggestion_url = table.Column<string>(type: "text", nullable: true),
                    suggestion_text = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    recipe_id = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_suggestions", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_suggestions_users_suggested_by",
                        column: x => x.suggested_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipe_suggestions_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_meal_plan_slots_recipe_id",
                table: "meal_plan_slots",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_suggestions_suggested_by",
                table: "recipe_suggestions",
                column: "suggested_by");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_suggestions_recipe_id",
                table: "recipe_suggestions",
                column: "recipe_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "meal_plan_slots");
            migrationBuilder.DropTable(name: "recipe_suggestions");
        }
    }
}
