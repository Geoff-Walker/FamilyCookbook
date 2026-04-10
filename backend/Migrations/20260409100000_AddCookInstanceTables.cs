using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WalkerFcb.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WalkerFcb.Api.Data.WalkerDbContext))]
    [Migration("20260409100000_AddCookInstanceTables")]
    public partial class AddCookInstanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cook_instances",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recipe_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    portions = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cook_instances", x => x.id);
                    table.ForeignKey(
                        name: "fk_cook_instances_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cook_instances_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cook_instance_ingredients",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cook_instance_id = table.Column<int>(type: "integer", nullable: false),
                    ingredient_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    unit_id = table.Column<int>(type: "integer", nullable: true),
                    @checked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_limiter = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cook_instance_ingredients", x => x.id);
                    table.ForeignKey(
                        name: "fk_cook_instance_ingredients_cook_instances_cook_instance_id",
                        column: x => x.cook_instance_id,
                        principalTable: "cook_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cook_instance_ingredients_ingredients_ingredient_id",
                        column: x => x.ingredient_id,
                        principalTable: "ingredients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cook_instance_ingredients_units_unit_id",
                        column: x => x.unit_id,
                        principalTable: "units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cook_instances_recipe_id",
                table: "cook_instances",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_cook_instances_user_id",
                table: "cook_instances",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_cook_instance_ingredients_cook_instance_id",
                table: "cook_instance_ingredients",
                column: "cook_instance_id");

            migrationBuilder.CreateIndex(
                name: "ix_cook_instance_ingredients_ingredient_id",
                table: "cook_instance_ingredients",
                column: "ingredient_id");

            migrationBuilder.CreateIndex(
                name: "ix_cook_instance_ingredients_unit_id",
                table: "cook_instance_ingredients",
                column: "unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop child table first (FK dependency)
            migrationBuilder.DropTable(name: "cook_instance_ingredients");
            migrationBuilder.DropTable(name: "cook_instances");
        }
    }
}
