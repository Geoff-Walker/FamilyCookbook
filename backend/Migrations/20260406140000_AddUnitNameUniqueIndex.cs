using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WalkerFcb.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WalkerFcb.Api.Data.WalkerDbContext))]
    [Migration("20260406140000_AddUnitNameUniqueIndex")]
    public partial class AddUnitNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_units_name",
                table: "units",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_units_name",
                table: "units");
        }
    }
}
