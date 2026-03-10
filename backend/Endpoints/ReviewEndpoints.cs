using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for recipe reviews.
/// All routes are nested under /api/recipes/:recipeId/reviews.
/// </summary>
public static class ReviewEndpoints
{
    public static WebApplication MapReviewEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/recipes/{recipeId:int}/reviews")
            .WithTags("Reviews");

        // POST /api/recipes/{recipeId}/reviews
        group.MapPost("/", CreateReview)
            .WithSummary("Add a review for a recipe")
            .Produces<ReviewDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        // GET /api/recipes/{recipeId}/reviews
        group.MapGet("/", GetReviews)
            .WithSummary("List all reviews for a recipe, ordered by date descending")
            .Produces<List<ReviewDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    // -----------------------------------------------------------------------
    // Handlers
    // -----------------------------------------------------------------------

    private static async Task<IResult> CreateReview(
        int recipeId,
        CreateReviewDto request,
        WalkerDbContext db)
    {
        // AC2: rating must be 1–5
        if (request.Rating < 1 || request.Rating > 5)
            return Results.BadRequest(new { error = "rating must be an integer between 1 and 5" });

        // AC5: parse madeOn if provided
        DateOnly? madeOn = null;
        if (request.MadeOn != null)
        {
            if (!DateOnly.TryParseExact(request.MadeOn, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var parsedDate))
            {
                return Results.BadRequest(new { error = "madeOn must be a valid date (ISO 8601)" });
            }
            madeOn = parsedDate;
        }

        // AC4: recipe must exist and not be soft-deleted (query filter handles soft-delete)
        var recipeExists = await db.Recipes
            .AnyAsync(r => r.Id == recipeId);

        if (!recipeExists)
            return Results.NotFound();

        // AC3: userId must match an existing user
        var user = await db.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => new { u.Id, u.Name })
            .FirstOrDefaultAsync();

        if (user == null)
            return Results.BadRequest(new { error = "invalid userId" });

        // Persist the review
        var review = new RecipeReview
        {
            RecipeId  = recipeId,
            UserId    = request.UserId,
            Rating    = request.Rating,
            Notes     = request.Notes,
            MadeOn    = madeOn,
            CreatedAt = DateTime.UtcNow,
        };

        db.RecipeReviews.Add(review);
        await db.SaveChangesAsync();

        var dto = new ReviewDto
        {
            Id        = review.Id,
            RecipeId  = review.RecipeId,
            UserId    = review.UserId,
            UserName  = user.Name,
            Rating    = review.Rating,
            Notes     = review.Notes,
            MadeOn    = review.MadeOn,
            CreatedAt = review.CreatedAt,
        };

        // AC1: 201 with Location header pointing at the new resource
        return Results.Created($"/api/recipes/{recipeId}/reviews/{review.Id}", dto);
    }

    private static async Task<IResult> GetReviews(
        int recipeId,
        WalkerDbContext db)
    {
        // AC4: recipe must exist and not be soft-deleted
        var recipeExists = await db.Recipes
            .AnyAsync(r => r.Id == recipeId);

        if (!recipeExists)
            return Results.NotFound();

        // AC6: return reviews ordered by created_at descending; empty array if none
        var reviews = await db.RecipeReviews
            .Where(rr => rr.RecipeId == recipeId)
            .Include(rr => rr.User)
            .OrderByDescending(rr => rr.CreatedAt)
            .Select(rr => new ReviewDto
            {
                Id        = rr.Id,
                RecipeId  = rr.RecipeId,
                UserId    = rr.UserId,
                UserName  = rr.User.Name,
                Rating    = rr.Rating,
                Notes     = rr.Notes,
                MadeOn    = rr.MadeOn,
                CreatedAt = rr.CreatedAt,
            })
            .ToListAsync();

        return Results.Ok(reviews);
    }
}
