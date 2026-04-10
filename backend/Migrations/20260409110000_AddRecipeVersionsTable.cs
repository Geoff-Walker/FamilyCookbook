using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WalkerFcb.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WalkerFcb.Api.Data.WalkerDbContext))]
    [Migration("20260409110000_AddRecipeVersionsTable")]
    public partial class AddRecipeVersionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recipe_versions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    recipe_id = table.Column<int>(type: "integer", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    promoted_from = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recipe_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_recipe_versions_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recipe_versions_cook_instances_promoted_from",
                        column: x => x.promoted_from,
                        principalTable: "cook_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recipe_versions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_versions_recipe_id",
                table: "recipe_versions",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_versions_promoted_from",
                table: "recipe_versions",
                column: "promoted_from");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_versions_created_by",
                table: "recipe_versions",
                column: "created_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "recipe_versions");
        }
    }
}
