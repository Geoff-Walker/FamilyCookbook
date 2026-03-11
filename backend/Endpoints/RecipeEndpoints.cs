using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.DTOs;
using WalkerFcb.Api.Services;

namespace WalkerFcb.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for recipe CRUD operations.
/// All routes are under /api/recipes.
/// </summary>
public static class RecipeEndpoints
{
    public static WebApplication MapRecipeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recipes")
            .WithTags("Recipes");

        // GET /api/recipes
        group.MapGet("/", GetAllRecipes)
            .WithSummary("List all recipes")
            .Produces<List<RecipeSummaryDto>>(StatusCodes.Status200OK);

        // GET /api/recipes/filter?ingredientIds=1,2,3
        group.MapGet("/filter", FilterByIngredients)
            .WithSummary("Filter recipes by ingredient IDs (returns recipes containing ALL specified ingredients)")
            .Produces<List<RecipeSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // GET /api/recipes/{id}
        group.MapGet("/{id:int}", GetRecipeById)
            .WithSummary("Get recipe detail")
            .Produces<RecipeDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/recipes
        group.MapPost("/", CreateRecipe)
            .WithSummary("Create a recipe")
            .Produces<RecipeDetailDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        // PUT /api/recipes/{id}
        group.MapPut("/{id:int}", UpdateRecipe)
            .WithSummary("Update a recipe (full replace)")
            .Produces<RecipeDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/recipes/{id}
        group.MapDelete("/{id:int}", DeleteRecipe)
            .WithSummary("Soft-delete a recipe")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // -----------------------------------------------------------------------
    // Handlers
    // -----------------------------------------------------------------------

    private static async Task<IResult> GetAllRecipes(RecipeService service)
    {
        var recipes = await service.GetAllAsync();
        return Results.Ok(recipes);
    }

    /// <summary>
    /// Returns recipes that contain ALL of the specified ingredient IDs.
    /// The ingredientIds query parameter accepts a comma-separated list of integers,
    /// e.g. GET /api/recipes/filter?ingredientIds=1,2,3
    /// </summary>
    private static async Task<IResult> FilterByIngredients(
        string? ingredientIds,
        WalkerDbContext db)
    {
        if (string.IsNullOrWhiteSpace(ingredientIds))
            return Results.BadRequest(new { error = "ingredientIds is required" });

        // Parse comma-separated list; reject on any non-integer token
        var rawTokens = ingredientIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ids = new List<int>(rawTokens.Length);
        foreach (var token in rawTokens)
        {
            if (!int.TryParse(token, out var id) || id <= 0)
                return Results.BadRequest(new { error = $"Invalid ingredient ID: '{token}'" });
            ids.Add(id);
        }

        if (ids.Count == 0)
            return Results.BadRequest(new { error = "ingredientIds must contain at least one valid ID" });

        // Find recipe IDs that contain ALL specified ingredients.
        // We count distinct matching ingredient IDs per recipe and keep only recipes
        // where the count equals the number of requested IDs.
        var matchingRecipeIds = await db.RecipeIngredients
            .AsNoTracking()
            .Where(ri => ids.Contains(ri.IngredientId))
            .GroupBy(ri => ri.RecipeId)
            .Where(g => g.Select(ri => ri.IngredientId).Distinct().Count() == ids.Count)
            .Select(g => g.Key)
            .ToListAsync();

        if (matchingRecipeIds.Count == 0)
            return Results.Ok(new List<RecipeSummaryDto>());

        var recipes = await db.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeTags)
                .ThenInclude(rt => rt.Tag)
                    .ThenInclude(t => t.Category)
            .Include(r => r.Reviews)
                .ThenInclude(rv => rv.User)
            .Where(r => matchingRecipeIds.Contains(r.Id))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = recipes.Select(r => new RecipeSummaryDto
        {
            Id = r.Id,
            Title = r.Title,
            PrepTimeMinutes = r.PrepTimeMinutes,
            CookTimeMinutes = r.CookTimeMinutes,
            Tags = r.RecipeTags
                .OrderBy(rt => rt.Tag.Category.Name)
                .ThenBy(rt => rt.Tag.Name)
                .Select(rt => new RecipeSummaryTagDto
                {
                    Id = rt.TagId,
                    Name = rt.Tag.Name,
                    CategoryName = rt.Tag.Category.Name
                }).ToList(),
            Ratings = r.Reviews
                .GroupBy(rv => new { rv.UserId, rv.User.Name })
                .Select(g => new RecipeSummaryRatingDto
                {
                    UserId = g.Key.UserId,
                    UserName = g.Key.Name,
                    AverageRating = g.Average(rv => (double)rv.Rating)
                }).ToList()
        }).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetRecipeById(int id, RecipeService service)
    {
        var recipe = await service.GetByIdAsync(id);
        return recipe == null
            ? Results.NotFound()
            : Results.Ok(recipe);
    }

    private static async Task<IResult> CreateRecipe(
        CreateRecipeDto request,
        RecipeService service,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(new { error = "title is required" });

        var (dto, embeddingFailed) = await service.CreateAsync(request);

        if (embeddingFailed)
            httpContext.Response.Headers["X-Embedding-Status"] = "failed";

        return Results.Created($"/api/recipes/{dto.Id}", dto);
    }

    private static async Task<IResult> UpdateRecipe(
        int id,
        UpdateRecipeDto request,
        RecipeService service,
        HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(new { error = "title is required" });

        var (dto, embeddingFailed) = await service.UpdateAsync(id, request);

        if (dto == null)
            return Results.NotFound();

        if (embeddingFailed)
            httpContext.Response.Headers["X-Embedding-Status"] = "failed";

        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteRecipe(int id, RecipeService service)
    {
        var found = await service.SoftDeleteAsync(id);
        return found
            ? Results.NoContent()
            : Results.NotFound();
    }
}
