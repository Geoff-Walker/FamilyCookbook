using WalkerFcb.Api.DTOs;
using WalkerFcb.Api.Services;
using Microsoft.AspNetCore.Http;

namespace WalkerFcb.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for The Geoff Filter — the recipe suggestion queue.
/// Route: /api/recipe-suggestions
/// </summary>
public static class RecipeSuggestionEndpoints
{
    public static WebApplication MapRecipeSuggestionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recipe-suggestions")
            .WithTags("RecipeSuggestions");

        // GET /api/recipe-suggestions?status=pending|backlogged
        group.MapGet("/", GetByStatus)
            .WithSummary("Return suggestions by status (pending or backlogged), ordered oldest first")
            .Produces<List<RecipeSuggestionDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // POST /api/recipe-suggestions
        group.MapPost("/", Create)
            .WithSummary("Submit a new recipe suggestion; at least one of suggestionUrl or suggestionText required")
            .Produces<RecipeSuggestionDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // PATCH /api/recipe-suggestions/{id}/backlog
        group.MapPatch("/{id:int}/backlog", Backlog)
            .WithSummary("Move a suggestion to the backlog")
            .Produces<RecipeSuggestionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/recipe-suggestions/{id}
        group.MapDelete("/{id:int}", SoftDelete)
            .WithSummary("Soft-delete a suggestion (sets status = 'deleted'); no ownership check")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/recipe-suggestions/{id}/accept
        group.MapPost("/{id:int}/accept", Accept)
            .WithSummary("Accept a suggestion — restricted to Geoff (user ID 1). Creates a stub recipe.")
            .Produces<AcceptRecipeSuggestionResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // -----------------------------------------------------------------------
    // Handlers
    // -----------------------------------------------------------------------

    private static async Task<IResult> GetByStatus(
        string? status,
        RecipeSuggestionService service)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Results.BadRequest(new { error = "'status' query parameter is required (pending or backlogged)" });

        if (status != "pending" && status != "backlogged")
            return Results.BadRequest(new { error = $"'status' value '{status}' is not valid. Expected 'pending' or 'backlogged'." });

        var suggestions = await service.GetByStatusAsync(status);
        return Results.Ok(suggestions);
    }

    private static async Task<IResult> Create(
        CreateRecipeSuggestionDto request,
        RecipeSuggestionService service)
    {
        var (dto, validationError, notFound) = await service.CreateAsync(request);

        if (validationError != null)
            return Results.BadRequest(new { error = validationError });

        if (notFound)
            return Results.NotFound(new { error = "The specified user does not exist." });

        return Results.Created($"/api/recipe-suggestions/{dto!.Id}", dto);
    }

    private static async Task<IResult> Backlog(
        int id,
        RecipeSuggestionService service)
    {
        var dto = await service.BacklogAsync(id);
        return dto == null
            ? Results.NotFound()
            : Results.Ok(dto);
    }

    private static async Task<IResult> SoftDelete(
        int id,
        RecipeSuggestionService service)
    {
        var found = await service.SoftDeleteAsync(id);
        return found
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> Accept(
        int id,
        AcceptRecipeSuggestionDto request,
        RecipeSuggestionService service)
    {
        var (result, forbidden, notFound) = await service.AcceptAsync(id, request.RequestingUserId);

        if (forbidden)
            return Results.Json(
                new { error = "Only Geoff can accept suggestions." },
                statusCode: StatusCodes.Status403Forbidden);

        if (notFound)
            return Results.NotFound();

        return Results.Ok(result);
    }
}
