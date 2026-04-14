using WalkerFcb.Api.DTOs;
using WalkerFcb.Api.Services;
using Microsoft.AspNetCore.Http;

namespace WalkerFcb.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for the cook instance lifecycle.
/// Routes: /api/cook-instances and /api/recipes/{recipeId}/cook-instances.
/// </summary>
public static class CookInstanceEndpoints
{
    public static WebApplication MapCookInstanceEndpoints(this WebApplication app)
    {
        // -----------------------------------------------------------------------
        // Cook instance resource routes — /api/cook-instances
        // -----------------------------------------------------------------------
        var cookGroup = app.MapGroup("/api/cook-instances")
            .WithTags("CookInstances");

        // POST /api/cook-instances
        cookGroup.MapPost("/", StartCook)
            .WithSummary("Start a new cook instance; copies recipe_ingredients into cook_instance_ingredients")
            .Produces<CookInstanceDetailDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/cook-instances/{id}
        cookGroup.MapGet("/{id:int}", GetCookInstance)
            .WithSummary("Get cook instance detail with ingredients grouped by stage")
            .Produces<CookInstanceDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // PATCH /api/cook-instances/{id}/ingredients/{ingredientId}
        cookGroup.MapPatch("/{id:int}/ingredients/{ingredientId:int}", PatchIngredient)
            .WithSummary("Partial update of a cook instance ingredient (checked, amount, isLimiter)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/cook-instances/{id}/complete
        cookGroup.MapPost("/{id:int}/complete", CompleteCook)
            .WithSummary("Mark a cook as complete; optionally submit reviews")
            .Produces<CookInstanceDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/cook-instances/{id}
        cookGroup.MapDelete("/{id:int}", SoftDeleteCook)
            .WithSummary("Soft-delete a cook instance")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // POST /api/cook-instances/{id}/promote
        cookGroup.MapPost("/{id:int}/promote", PromoteCook)
            .WithSummary("Promote cook instance actuals to recipe ingredient baseline; snapshots previous state into recipe_versions")
            .Produces<PromoteResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

        // -----------------------------------------------------------------------
        // History route nested under recipes — /api/recipes/{recipeId}/cook-instances
        // -----------------------------------------------------------------------
        app.MapGet("/api/recipes/{recipeId:int}/cook-instances", GetCookHistory)
            .WithTags("CookInstances")
            .WithSummary("List cook instances for a recipe, ordered by started_at DESC")
            .Produces<CookHistoryResponseDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/recipes/{recipeId}/versions
        app.MapGet("/api/recipes/{recipeId:int}/versions", GetVersionHistory)
            .WithTags("CookInstances")
            .WithSummary("List version history for a recipe, ordered by version_number DESC")
            .Produces<List<RecipeVersionSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // -----------------------------------------------------------------------
    // Handlers
    // -----------------------------------------------------------------------

    private static async Task<IResult> StartCook(
        StartCookDto request,
        CookInstanceService service)
    {
        if (request.RecipeId <= 0)
            return Results.BadRequest(new { error = "recipeId is required" });

        if (request.UserId <= 0)
            return Results.BadRequest(new { error = "userId is required" });

        var dto = await service.StartCookAsync(request);

        if (dto == null)
            return Results.NotFound();

        return Results.Created($"/api/cook-instances/{dto.Id}", dto);
    }

    private static async Task<IResult> GetCookInstance(
        int id,
        CookInstanceService service)
    {
        var dto = await service.GetByIdAsync(id);
        return dto == null
            ? Results.NotFound()
            : Results.Ok(dto);
    }

    private static async Task<IResult> PatchIngredient(
        int id,
        int ingredientId,
        PatchCookInstanceIngredientDto patch,
        CookInstanceService service)
    {
        if (patch.Amount.HasValue && patch.Amount.Value < 0)
            return Results.BadRequest(new { error = "amount must be non-negative" });

        var found = await service.PatchIngredientAsync(id, ingredientId, patch);
        return found
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> CompleteCook(
        int id,
        CompleteCookDto request,
        CookInstanceService service)
    {
        var (dto, validationError) = await service.CompleteCookAsync(id, request);

        if (validationError != null)
            return Results.BadRequest(new { error = validationError });

        return dto == null
            ? Results.NotFound()
            : Results.Ok(dto);
    }

    private static async Task<IResult> SoftDeleteCook(
        int id,
        CookInstanceService service)
    {
        var found = await service.SoftDeleteAsync(id);
        return found
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> GetCookHistory(
        int recipeId,
        CookInstanceService service)
    {
        var history = await service.GetHistoryByRecipeAsync(recipeId);
        return history == null
            ? Results.NotFound()
            : Results.Ok(history);
    }

    private static async Task<IResult> PromoteCook(
        int id,
        HttpRequest request,
        CookInstanceService service)
    {
        // Active user passed via X-User-Id header (1 = Geoff, 2 = Helen)
        if (!int.TryParse(request.Headers["X-User-Id"].FirstOrDefault(), out var userId) || userId <= 0)
            return Results.BadRequest(new { error = "X-User-Id header is required and must be a valid user ID" });

        try
        {
            var (result, error, notFound) = await service.PromoteCookAsync(id, userId);

            if (notFound)
                return Results.NotFound();

            if (error != null)
                return Results.BadRequest(new { error });

            return Results.Ok(result);
        }
        catch
        {
            return Results.Json(
                new { error = "Promote failed. No changes were made." },
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetVersionHistory(
        int recipeId,
        CookInstanceService service)
    {
        var versions = await service.GetVersionsByRecipeAsync(recipeId);
        return versions == null
            ? Results.NotFound()
            : Results.Ok(versions);
    }
}
