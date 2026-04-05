using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.DTOs;
using WalkerFcb.Api.Services;
using Microsoft.AspNetCore.Http.Features;

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

        // GET /api/recipes/filter?ingredientIds=1,2,3&tagIds=4,5
        group.MapGet("/filter", FilterByIngredientsAndTags)
            .WithSummary("Filter recipes by ingredient IDs and/or tag IDs (ingredientIds: ALL must match; tagIds: AND across categories, OR within same category)")
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

        // POST /api/recipes/{id}/image
        group.MapPost("/{id:int}/image", UploadImage)
            .WithSummary("Upload a recipe hero image (JPEG or PNG, max 5 MB)")
            .Produces<ImageUploadResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .DisableAntiforgery();

        // POST /api/recipes/{id}/image/generate
        group.MapPost("/{id:int}/image/generate", GenerateImage)
            .WithSummary("Generate a recipe hero image via OpenAI gpt-image-1")
            .Produces<GenerateImageResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status502BadGateway);

        // POST /api/recipes/{id}/image/idealise
        group.MapPost("/{id:int}/image/idealise", IdealiseImage)
            .WithSummary("Idealise an existing recipe image via OpenAI gpt-image-1 img2img")
            .Produces<IdealiseImageResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status502BadGateway);

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
    /// Returns recipes matching optional ingredient IDs and/or tag IDs.
    ///
    /// Ingredient logic: recipe must contain ALL specified ingredient IDs.
    /// Tag logic (AC9): tags from different categories are ANDed; tags within the
    /// same category are ORed. Example: "Italian"(cuisine) + "French"(cuisine)
    /// + "Vegetarian"(dietary) → (Italian OR French) AND Vegetarian.
    ///
    /// At least one of ingredientIds or tagIds must be provided.
    /// e.g. GET /api/recipes/filter?ingredientIds=1,2&tagIds=3,4
    /// </summary>
    private static async Task<IResult> FilterByIngredientsAndTags(
        string? ingredientIds,
        string? tagIds,
        WalkerDbContext db)
    {
        // At least one filter is required
        if (string.IsNullOrWhiteSpace(ingredientIds) && string.IsNullOrWhiteSpace(tagIds))
            return Results.BadRequest(new { error = "At least one of ingredientIds or tagIds is required" });

        // ---- Parse ingredient IDs ----
        List<int>? parsedIngredientIds = null;
        if (!string.IsNullOrWhiteSpace(ingredientIds))
        {
            parsedIngredientIds = new List<int>();
            foreach (var token in ingredientIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!int.TryParse(token, out var id) || id <= 0)
                    return Results.BadRequest(new { error = $"Invalid ingredient ID: '{token}'" });
                parsedIngredientIds.Add(id);
            }
        }

        // ---- Parse tag IDs ----
        List<int>? parsedTagIds = null;
        if (!string.IsNullOrWhiteSpace(tagIds))
        {
            parsedTagIds = new List<int>();
            foreach (var token in tagIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!int.TryParse(token, out var id) || id <= 0)
                    return Results.BadRequest(new { error = $"Invalid tag ID: '{token}'" });
                parsedTagIds.Add(id);
            }
        }

        // ---- Ingredient filter: recipe must contain ALL specified ingredients ----
        HashSet<int>? ingredientFilteredIds = null;
        if (parsedIngredientIds is { Count: > 0 })
        {
            var matchingIds = await db.RecipeIngredients
                .AsNoTracking()
                .Where(ri => parsedIngredientIds.Contains(ri.IngredientId))
                .GroupBy(ri => ri.RecipeId)
                .Where(g => g.Select(ri => ri.IngredientId).Distinct().Count() == parsedIngredientIds.Count)
                .Select(g => g.Key)
                .ToListAsync();

            if (matchingIds.Count == 0)
                return Results.Ok(new List<RecipeSummaryDto>());

            ingredientFilteredIds = matchingIds.ToHashSet();
        }

        // ---- Tag filter: AND across categories, OR within same category ----
        // Load the requested tags with their category IDs so we can group them.
        HashSet<int>? tagFilteredIds = null;
        if (parsedTagIds is { Count: > 0 })
        {
            // Load requested tags to discover their categories
            var requestedTags = await db.Tags
                .AsNoTracking()
                .Where(t => parsedTagIds.Contains(t.Id))
                .Select(t => new { t.Id, t.CategoryId })
                .ToListAsync();

            // Group by category: within each category, a recipe matches if it has ANY of the tags.
            // Across categories, the recipe must satisfy ALL category groups.
            var tagsByCategory = requestedTags
                .GroupBy(t => t.CategoryId)
                .Select(g => g.Select(t => t.Id).ToList())
                .ToList();

            // Start with all recipe IDs, then intersect per category group.
            HashSet<int>? accumulator = null;
            foreach (var categoryTagIds in tagsByCategory)
            {
                var recipeIdsForCategory = await db.RecipeTags
                    .AsNoTracking()
                    .Where(rt => categoryTagIds.Contains(rt.TagId))
                    .Select(rt => rt.RecipeId)
                    .Distinct()
                    .ToListAsync();

                if (accumulator is null)
                    accumulator = recipeIdsForCategory.ToHashSet();
                else
                    accumulator.IntersectWith(recipeIdsForCategory);

                if (accumulator.Count == 0)
                    return Results.Ok(new List<RecipeSummaryDto>());
            }

            tagFilteredIds = accumulator;
        }

        // ---- Intersect ingredient and tag result sets ----
        HashSet<int>? combinedIds = null;

        if (ingredientFilteredIds is not null && tagFilteredIds is not null)
        {
            ingredientFilteredIds.IntersectWith(tagFilteredIds);
            combinedIds = ingredientFilteredIds;
        }
        else
        {
            combinedIds = ingredientFilteredIds ?? tagFilteredIds;
        }

        if (combinedIds is null || combinedIds.Count == 0)
            return Results.Ok(new List<RecipeSummaryDto>());

        // ---- Load and project matching recipes ----
        var recipes = await db.Recipes
            .AsNoTracking()
            .Include(r => r.RecipeTags)
                .ThenInclude(rt => rt.Tag)
                    .ThenInclude(t => t.Category)
            .Include(r => r.Reviews)
                .ThenInclude(rv => rv.User)
            .Where(r => combinedIds.Contains(r.Id))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = recipes.Select(r => new RecipeSummaryDto
        {
            Id = r.Id,
            Title = r.Title,
            PrepTimeMinutes = r.PrepTimeMinutes,
            CookTimeMinutes = r.CookTimeMinutes,
            Servings = r.Servings,
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

        if (request.Servings.HasValue && request.Servings.Value < 1)
            return Results.BadRequest(new { error = "servings must be a positive integer (≥ 1)" });

        foreach (var stage in request.Stages)
        {
            foreach (var ingredient in stage.Ingredients)
            {
                if (ingredient.WeightGrams.HasValue && ingredient.WeightGrams.Value <= 0)
                    return Results.BadRequest(new { error = "weightGrams must be a positive value (> 0)" });
            }
        }

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

        if (request.Servings.HasValue && request.Servings.Value < 1)
            return Results.BadRequest(new { error = "servings must be a positive integer (≥ 1)" });

        foreach (var stage in request.Stages)
        {
            foreach (var ingredient in stage.Ingredients)
            {
                if (ingredient.WeightGrams.HasValue && ingredient.WeightGrams.Value <= 0)
                    return Results.BadRequest(new { error = "weightGrams must be a positive value (> 0)" });
            }
        }

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

    private static async Task<IResult> UploadImage(
        int id,
        IFormFile file,
        ImageUploadService imageUploadService)
    {
        var result = await imageUploadService.UploadAsync(id, file);

        if (result.RecipeNotFound)
            return Results.NotFound();

        if (!result.Succeeded)
            return Results.BadRequest(new { error = result.ErrorMessage });

        return Results.Ok(new ImageUploadResponseDto { ImageUrl = result.PublicUrl! });
    }

    private static async Task<IResult> GenerateImage(
        int id,
        GenerateImageRequestDto request,
        ImageGenerationService imageGenerationService)
    {
        // Validate required fields before calling the service so we can return
        // informative 400s without touching the DB.
        if (string.IsNullOrWhiteSpace(request.Description))
            return Results.BadRequest(new { error = "description is required." });

        if (request.Ingredients is not { Count: > 0 })
            return Results.BadRequest(new { error = "ingredients must contain at least one entry." });

        if (string.IsNullOrWhiteSpace(request.Style))
            return Results.BadRequest(new { error = $"style is required. Must be one of: {string.Join(", ", ImageGenerationService.ValidStyles)}." });

        var result = await imageGenerationService.GenerateAsync(
            id,
            request.Description,
            request.Ingredients,
            request.Style,
            request.FreeText);

        if (result.RecipeNotFound)
            return Results.NotFound();

        if (result.IsOpenAiError)
            return Results.Json(new { error = result.ErrorMessage }, statusCode: StatusCodes.Status502BadGateway);

        if (!result.Succeeded)
            return Results.BadRequest(new { error = result.ErrorMessage });

        return Results.Ok(new GenerateImageResponseDto { ImageUrl = result.PublicUrl! });
    }

    private static async Task<IResult> IdealiseImage(
        int id,
        IdealiseImageRequestDto request,
        ImageIdealiseService imageIdealiseService)
    {
        if (string.IsNullOrWhiteSpace(request.Style))
            return Results.BadRequest(new { error = $"style is required. Must be one of: {string.Join(", ", RecipeImageStyles.ValidStyles)}." });

        var result = await imageIdealiseService.IdealiseAsync(id, request.Style, request.FreeText);

        if (result.RecipeNotFound)
            return Results.NotFound();

        if (result.IsNoImageError)
            return Results.BadRequest(new { error = result.ErrorMessage });

        if (result.IsStyleError)
            return Results.BadRequest(new { error = result.ErrorMessage });

        if (result.IsOpenAiError)
            return Results.Json(new { error = result.ErrorMessage }, statusCode: StatusCodes.Status502BadGateway);

        if (!result.Succeeded)
            return Results.BadRequest(new { error = result.ErrorMessage });

        return Results.Ok(new IdealiseImageResponseDto { ImageUrl = result.PublicUrl! });
    }
}
