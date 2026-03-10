using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WalkerFcb.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "tag_categories",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Cuisine" },
                    { 2, "Dietary" },
                    { 3, "Occasion" },
                    { 4, "Season" }
                });

            migrationBuilder.InsertData(
                table: "units",
                columns: new[] { "id", "abbreviation", "name", "unit_type" },
                values: new object[,]
                {
                    { 1, "g", "gram", "mass" },
                    { 2, "kg", "kilogram", "mass" },
                    { 3, "oz", "ounce", "mass" },
                    { 4, "lb", "pound", "mass" },
                    { 5, "ml", "millilitre", "volume" },
                    { 6, "l", "litre", "volume" },
                    { 7, "tsp", "teaspoon", "volume" },
                    { 8, "tbsp", "tablespoon", "volume" },
                    { 9, "cup", "cup", "volume" },
                    { 10, "pinch", "pinch", "arbitrary" },
                    { 11, "handful", "handful", "arbitrary" },
                    { 12, "to taste", "to taste", "arbitrary" },
                    { 13, "x", "whole", "count" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Geoff" },
                    { 2, "Helen" }
                });

            migrationBuilder.InsertData(
                table: "tags",
                columns: new[] { "id", "category_id", "name", "slug" },
                values: new object[,]
                {
                    { 1, 1, "Italian", "italian" },
                    { 2, 1, "Chinese", "chinese" },
                    { 3, 1, "Indian", "indian" },
                    { 4, 1, "Mexican", "mexican" },
                    { 5, 1, "French", "french" },
                    { 6, 1, "Thai", "thai" },
                    { 7, 1, "Japanese", "japanese" },
                    { 8, 1, "British", "british" },
                    { 9, 2, "Vegetarian", "vegetarian" },
                    { 10, 2, "Vegan", "vegan" },
                    { 11, 2, "Gluten-Free", "gluten-free" },
                    { 12, 2, "Dairy-Free", "dairy-free" },
                    { 13, 2, "Nut-Free", "nut-free" },
                    { 14, 3, "Weeknight", "weeknight" },
                    { 15, 3, "Weekend", "weekend" },
                    { 16, 3, "Special Occasion", "special-occasion" },
                    { 17, 3, "Batch Cook", "batch-cook" },
                    { 18, 3, "Quick", "quick" },
                    { 19, 4, "Spring", "spring" },
                    { 20, 4, "Summer", "summer" },
                    { 21, 4, "Autumn", "autumn" },
                    { 22, 4, "Winter", "winter" }
                });

            // Advance identity sequences past the seeded IDs so that subsequent
            // auto-generated inserts do not collide with seed data.
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('users', 'id'), (SELECT MAX(id) FROM users));");
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('units', 'id'), (SELECT MAX(id) FROM units));");
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('tag_categories', 'id'), (SELECT MAX(id) FROM tag_categories));");
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('tags', 'id'), (SELECT MAX(id) FROM tags));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "tags",
                keyColumn: "id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "units",
                keyColumn: "id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tag_categories",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "tag_categories",
                keyColumn: "id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "tag_categories",
                keyColumn: "id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "tag_categories",
                keyColumn: "id",
                keyValue: 4);
        }
    }
}
