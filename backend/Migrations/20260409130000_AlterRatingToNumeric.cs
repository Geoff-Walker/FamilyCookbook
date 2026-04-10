using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalkerFcb.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WalkerFcb.Api.Data.WalkerDbContext))]
    [Migration("20260409130000_AlterRatingToNumeric")]
    public partial class AlterRatingToNumeric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "rating",
                table: "recipe_reviews",
                type: "numeric(3,1)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOTE: rolling back to integer will truncate half-star values (e.g. 3.5 → 3).
            // This is acceptable as rollback is an emergency-only operation.
            migrationBuilder.AlterColumn<int>(
                name: "rating",
                table: "recipe_reviews",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,1)");
        }
    }
}
