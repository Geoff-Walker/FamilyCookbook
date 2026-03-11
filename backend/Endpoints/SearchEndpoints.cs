using WalkerFcb.Api.DTOs;
using WalkerFcb.Api.Services;

namespace WalkerFcb.Api.Endpoints;

/// <summary>
/// Semantic search endpoint.
/// GET /api/search?query=:term&amp;userId=:userId
/// </summary>
public static class SearchEndpoints
{
    public static WebApplication MapSearchEndpoints(this WebApplication app)
    {
        app.MapGet("/api/search", Search)
            .WithTags("Search")
            .WithSummary("Semantic recipe search via pgvector cosine similarity")
            .Produces<List<RecipeSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> Search(
        string? query,
        int? userId,
        SearchService searchService)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Results.BadRequest(new { error = "query is required" });

        if (userId is null or <= 0)
            return Results.BadRequest(new { error = "userId is required and must be a positive integer" });

        try
        {
            var results = await searchService.SearchAsync(query.Trim(), userId.Value);
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
