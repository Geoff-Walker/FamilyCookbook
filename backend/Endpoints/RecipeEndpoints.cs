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
