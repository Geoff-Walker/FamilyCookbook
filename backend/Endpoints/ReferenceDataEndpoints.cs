using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for read-only reference data used by autocomplete,
/// dropdowns, and filter panels in the frontend.
/// All routes are read-only — no write endpoints are provided here.
/// </summary>
public static class ReferenceDataEndpoints
{
    public static WebApplication MapReferenceDataEndpoints(this WebApplication app)
    {
        // -----------------------------------------------------------------------
        // Ingredients
        // -----------------------------------------------------------------------
        app.MapGet("/api/ingredients", GetIngredients)
            .WithTags("Reference Data")
            .WithSummary("List all ingredients, optionally filtered by name")
            .Produces<List<IngredientDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // -----------------------------------------------------------------------
        // Units
        // -----------------------------------------------------------------------
        app.MapGet("/api/units", GetUnits)
            .WithTags("Reference Data")
            .WithSummary("List all units of measurement")
            .Produces<List<UnitDto>>(StatusCodes.Status200OK);

        // -----------------------------------------------------------------------
        // Tags
        // -----------------------------------------------------------------------
        app.MapGet("/api/tags", GetTags)
            .WithTags("Reference Data")
            .WithSummary("List all tags with their category")
            .Produces<List<TagDto>>(StatusCodes.Status200OK);

        // -----------------------------------------------------------------------
        // Tag categories
        // -----------------------------------------------------------------------
        app.MapGet("/api/tag-categories", GetTagCategories)
            .WithTags("Reference Data")
            .WithSummary("List all tag categories")
            .Produces<List<TagCategoryDto>>(StatusCodes.Status200OK);

        // -----------------------------------------------------------------------
        // Users
        // -----------------------------------------------------------------------
        app.MapGet("/api/users", GetUsers)
            .WithTags("Reference Data")
            .WithSummary("List all users")
            .Produces<List<UserDto>>(StatusCodes.Status200OK);

        return app;
    }

    // -----------------------------------------------------------------------
    // Handlers
    // -----------------------------------------------------------------------

    private static async Task<IResult> GetIngredients(
        WalkerDbContext db,
        string? search = null)
    {
        // AC2: 400 if search param is provided but is an empty string
        if (search != null && search.Length == 0)
            return Results.BadRequest(new { error = "search term must not be empty" });

        var query = db.Ingredients.AsQueryable();

        if (search != null)
        {
            // Case-insensitive contains using EF.Functions.ILike (PostgreSQL)
            query = query.Where(i => EF.Functions.ILike(i.Name, $"%{search}%"));
        }

        var ingredients = await query
            .OrderBy(i => i.Name)
            .Select(i => new IngredientDto { Id = i.Id, Name = i.Name })
            .ToListAsync();

        return Results.Ok(ingredients);
    }

    private static async Task<IResult> GetUnits(WalkerDbContext db)
    {
        var units = await db.Units
            .OrderBy(u => u.Name)
            .Select(u => new UnitDto
            {
                Id           = u.Id,
                Name         = u.Name,
                Abbreviation = u.Abbreviation,
                UnitType     = u.UnitType,
            })
            .ToListAsync();

        return Results.Ok(units);
    }

    private static async Task<IResult> GetTags(WalkerDbContext db)
    {
        var tags = await db.Tags
            .Include(t => t.Category)
            .OrderBy(t => t.Category.Name)
            .ThenBy(t => t.Name)
            .Select(t => new TagDto
            {
                Id           = t.Id,
                Name         = t.Name,
                Slug         = t.Slug,
                CategoryId   = t.CategoryId,
                CategoryName = t.Category.Name,
            })
            .ToListAsync();

        return Results.Ok(tags);
    }

    private static async Task<IResult> GetTagCategories(WalkerDbContext db)
    {
        var categories = await db.TagCategories
            .OrderBy(tc => tc.Name)
            .Select(tc => new TagCategoryDto { Id = tc.Id, Name = tc.Name })
            .ToListAsync();

        return Results.Ok(categories);
    }

    private static async Task<IResult> GetUsers(WalkerDbContext db)
    {
        var users = await db.Users
            .OrderBy(u => u.Id)
            .Select(u => new UserDto { Id = u.Id, Name = u.Name })
            .ToListAsync();

        return Results.Ok(users);
    }
}
