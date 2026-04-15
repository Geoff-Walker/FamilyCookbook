using WalkerFcb.Api.DTOs;
using WalkerFcb.Api.Services;
using Microsoft.AspNetCore.Http;

namespace WalkerFcb.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for meal plan slot CRUD.
/// Route: /api/meal-plan-slots
/// </summary>
public static class MealPlanSlotEndpoints
{
    public static WebApplication MapMealPlanSlotEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/meal-plan-slots")
            .WithTags("MealPlanSlots");

        // GET /api/meal-plan-slots?from=YYYY-MM-DD&to=YYYY-MM-DD
        group.MapGet("/", GetByDateRange)
            .WithSummary("Return all meal plan slots within a date range (inclusive). Both 'from' and 'to' are required.")
            .Produces<List<MealPlanSlotDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // POST /api/meal-plan-slots
        group.MapPost("/", Create)
            .WithSummary("Create a new meal plan slot")
            .Produces<MealPlanSlotDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // PATCH /api/meal-plan-slots/{id}
        group.MapPatch("/{id:int}", Patch)
            .WithSummary("Partial update of a meal plan slot")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/meal-plan-slots/{id}
        group.MapDelete("/{id:int}", Delete)
            .WithSummary("Hard-delete a meal plan slot")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // -----------------------------------------------------------------------
    // Handlers
    // -----------------------------------------------------------------------

    private static async Task<IResult> GetByDateRange(
        string? from,
        string? to,
        MealPlanSlotService service)
    {
        // AC 1 — both params are required
        if (string.IsNullOrWhiteSpace(from))
            return Results.BadRequest(new { error = "'from' query parameter is required (YYYY-MM-DD)" });

        if (string.IsNullOrWhiteSpace(to))
            return Results.BadRequest(new { error = "'to' query parameter is required (YYYY-MM-DD)" });

        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate))
            return Results.BadRequest(new { error = $"'from' value '{from}' is not a valid date (expected YYYY-MM-DD)" });

        if (!DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
            return Results.BadRequest(new { error = $"'to' value '{to}' is not a valid date (expected YYYY-MM-DD)" });

        if (fromDate > toDate)
            return Results.BadRequest(new { error = "'from' must not be later than 'to'" });

        var slots = await service.GetByDateRangeAsync(fromDate, toDate);
        return Results.Ok(slots);
    }

    private static async Task<IResult> Create(
        CreateMealPlanSlotDto request,
        MealPlanSlotService service)
    {
        var (dto, validationError, notFound) = await service.CreateAsync(request);

        if (validationError != null)
            return Results.BadRequest(new { error = validationError });

        if (notFound)
            return Results.NotFound(new { error = "The referenced recipe does not exist or has been deleted." });

        return Results.Created($"/api/meal-plan-slots/{dto!.Id}", dto);
    }

    private static async Task<IResult> Patch(
        int id,
        PatchMealPlanSlotDto patch,
        MealPlanSlotService service)
    {
        var (found, validationError) = await service.PatchAsync(id, patch);

        if (!found)
            return Results.NotFound();

        if (validationError != null)
            return Results.BadRequest(new { error = validationError });

        return Results.NoContent();
    }

    private static async Task<IResult> Delete(
        int id,
        MealPlanSlotService service)
    {
        var found = await service.DeleteAsync(id);
        return found
            ? Results.NoContent()
            : Results.NotFound();
    }
}
