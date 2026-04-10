using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalkerFcb.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WalkerFcb.Api.Data.WalkerDbContext))]
    [Migration("20260406130000_NormaliseIngredientNamesToLowercase")]
    public partial class NormaliseIngredientNamesToLowercase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only migration: convert all existing ingredient names to lowercase
            // so that stored values match the normalisation applied by the service layer
            // (ToLowerInvariant on write) going forward.
            //
            // migrationBuilder.UpdateData is not practical here because the ingredients
            // table is user-populated with an arbitrary number of rows — there is no
            // fixed seed set to enumerate. A targeted SQL UPDATE is the correct tool
            // for this kind of bulk data normalisation migration.
            migrationBuilder.Sql("UPDATE ingredients SET name = LOWER(name);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Lowercasing is not reversible without the original casing data.
            // Down() is intentionally left as a no-op.
        }
    }
}
