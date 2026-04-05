using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalkerFcb.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWeightGramsToRecipeIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // recipes.servings — present in InitialSchema for fresh installs.
            // Guard added for safety on any environment where the column may have
            // been dropped or was never migrated through the full history.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_name = 'recipes'
                          AND column_name = 'servings'
                    ) THEN
                        ALTER TABLE recipes ADD COLUMN servings integer NULL;
                    END IF;
                END
                $$;
            ");

            // recipe_ingredients.weight_grams — new column for Phase 2 portion control.
            migrationBuilder.AddColumn<decimal>(
                name: "weight_grams",
                table: "recipe_ingredients",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "weight_grams",
                table: "recipe_ingredients");

            // Do not drop recipes.servings in Down — it was present before this
            // migration and its removal would be destructive beyond this ticket's scope.
        }
    }
}
