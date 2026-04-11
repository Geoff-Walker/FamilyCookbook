using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalkerFcb.Api.Migrations
{
    /// <summary>
    /// Adds a nullable cook_instance_id FK to recipe_reviews so that reviews
    /// submitted via the complete-cook flow are linked back to the specific cook
    /// instance they belong to.
    /// </summary>
    [DbContext(typeof(WalkerFcb.Api.Data.WalkerDbContext))]
    [Migration("20260412100000_AddCookInstanceIdToRecipeReviews")]
    public partial class AddCookInstanceIdToRecipeReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cook_instance_id",
                table: "recipe_reviews",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_reviews_cook_instance_id",
                table: "recipe_reviews",
                column: "cook_instance_id");

            migrationBuilder.AddForeignKey(
                name: "fk_recipe_reviews_cook_instances_cook_instance_id",
                table: "recipe_reviews",
                column: "cook_instance_id",
                principalTable: "cook_instances",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_recipe_reviews_cook_instances_cook_instance_id",
                table: "recipe_reviews");

            migrationBuilder.DropIndex(
                name: "ix_recipe_reviews_cook_instance_id",
                table: "recipe_reviews");

            migrationBuilder.DropColumn(
                name: "cook_instance_id",
                table: "recipe_reviews");
        }
    }
}
