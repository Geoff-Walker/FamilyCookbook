using WalkerFcb.Api.DTOs;
using WalkerFcb.Api.Services;

namespace WalkerFcb.Api.Endpoints;

/// <summary>
/// Semantic search endpoint.
/// GET /api/search?query=:term&amp;userId=:userId
/// GET /api/search?query=:term&amp;userId=:userId&amp;ingredientIds=1,2,3  (combined semantic + ingredient filter)
/// GET /api/search?query=:term&amp;userId=:userId&amp;tagIds=4,5            (combined semantic + tag filter)
/// GET /api/search?query=:term&amp;userId=:userId&amp;ingredientIds=1,2&amp;tagIds=4,5  (all three)
/// </summary>
public static class SearchEndpoints
{
    public static WebApplication MapSearchEndpoints(this WebApplication app)
    {
        app.MapGet("/api/search", Search)
            .WithTags("Search")
            .WithSummary("Semantic recipe search, optionally filtered by ingredient IDs and/or tag IDs")
            .Produces<List<RecipeSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> Search(
        string? query,
        int? userId,
        string? ingredientIds,
        string? tagIds,
        SearchService searchService)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Results.BadRequest(new { error = "query is required" });

        if (userId is null or <= 0)
            return Results.BadRequest(new { error = "userId is required and must be a positive integer" });

        // Parse optional ingredient IDs
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

        // Parse optional tag IDs
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

        try
        {
            var results = await searchService.SearchAsync(query.Trim(), userId.Value, parsedIngredientIds, parsedTagIds);
            return Results.Ok(results);
        }
        catch (SearchUnavailableException ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Search unavailable");
        }
    }
}
