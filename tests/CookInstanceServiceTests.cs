using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.DTOs;
using WalkerFcb.Api.Services;

namespace WalkerFcb.Tests;

/// <summary>
/// Unit tests for CookInstanceService business logic.
/// Uses EF Core InMemory provider — Postgres-specific features (query filters, etc.)
/// are not exercised here; integration tests on the real DB would be needed for those.
///
/// The WalkerDbContext has HasQueryFilter(r => r.DeletedAt == null) on Recipes which
/// requires the InMemory provider to honour query filters. We configure the context
/// with the EF global query filter enabled (default), so tests must seed non-deleted recipes.
/// </summary>
public class CookInstanceServiceTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static WalkerDbContext BuildDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<WalkerDbContext>()
            .UseInMemoryDatabase(dbName)
            // UseSnakeCaseNamingConvention is an Npgsql extension not available on InMemory;
            // column names are not relevant here as we query via EF navigation, not raw SQL.
            .Options;
        return new WalkerDbContext(options);
    }

    private static async Task<(Recipe recipe, User user)> SeedMinimalRecipeAsync(
        WalkerDbContext db, string recipeName = "Test Recipe")
    {
        var user = new User { Name = "Geoff" };
        var ingredient = new Ingredient { Name = "Flour" };
        var recipe = new Recipe
        {
            Title = recipeName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.Ingredients.Add(ingredient);
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        // Add a recipe ingredient
        db.RecipeIngredients.Add(new RecipeIngredient
        {
            RecipeId = recipe.Id,
            IngredientId = ingredient.Id,
            Amount = "200",
            SortOrder = 0
        });
        await db.SaveChangesAsync();

        return (recipe, user);
    }

    // -----------------------------------------------------------------------
    // CompleteCookAsync — rating validation
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.5)]
    [InlineData(4.5)]
    [InlineData(5.0)]
    public async Task CompleteCook_ValidRatings_AreAccepted(decimal rating)
    {
        var db = BuildDb($"valid-rating-{rating}");
        var (recipe, user) = await SeedMinimalRecipeAsync(db);

        var cookInstance = new CookInstance
        {
            RecipeId = recipe.Id,
            UserId = user.Id,
            StartedAt = DateTime.UtcNow
        };
        db.CookInstances.Add(cookInstance);
        await db.SaveChangesAsync();

        var service = new CookInstanceService(db);
        var request = new CompleteCookDto
        {
            Reviews = [new CookReviewDto { UserId = user.Id, Rating = rating }]
        };

        var (dto, error) = await service.CompleteCookAsync(cookInstance.Id, request);

        Assert.Null(error);
        // dto may be null because InMemory doesn't honour query filters the same way —
        // but the validation path (error == null) is what we're testing here.
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(0.3)]
    [InlineData(1.1)]
    [InlineData(5.5)]
    [InlineData(6.0)]
    public async Task CompleteCook_InvalidRatings_ReturnValidationError(decimal rating)
    {
        var db = BuildDb($"invalid-rating-{rating}");
        var (recipe, user) = await SeedMinimalRecipeAsync(db);

        var cookInstance = new CookInstance
        {
            RecipeId = recipe.Id,
            UserId = user.Id,
            StartedAt = DateTime.UtcNow
        };
        db.CookInstances.Add(cookInstance);
        await db.SaveChangesAsync();

        var service = new CookInstanceService(db);
        var request = new CompleteCookDto
        {
            Reviews = [new CookReviewDto { UserId = user.Id, Rating = rating }]
        };

        var (dto, error) = await service.CompleteCookAsync(cookInstance.Id, request);

        Assert.NotNull(error);
        Assert.Contains(rating.ToString(), error);
    }

    // -----------------------------------------------------------------------
    // SoftDeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SoftDelete_ExistingCook_SetsDeletedAt()
    {
        var db = BuildDb("soft-delete-sets-deleted-at");
        var (recipe, user) = await SeedMinimalRecipeAsync(db);

        var cookInstance = new CookInstance
        {
            RecipeId = recipe.Id,
            UserId = user.Id,
            StartedAt = DateTime.UtcNow
        };
        db.CookInstances.Add(cookInstance);
        await db.SaveChangesAsync();

        var service = new CookInstanceService(db);
        var result = await service.SoftDeleteAsync(cookInstance.Id);

        Assert.True(result);

        // Re-load bypassing the service to inspect the raw entity
        var raw = await db.CookInstances.FindAsync(cookInstance.Id);
        Assert.NotNull(raw?.DeletedAt);
    }

    [Fact]
    public async Task SoftDelete_NonExistentCook_ReturnsFalse()
    {
        var db = BuildDb("soft-delete-not-found");
        var service = new CookInstanceService(db);

        var result = await service.SoftDeleteAsync(99999);

        Assert.False(result);
    }

    [Fact]
    public async Task SoftDelete_AlreadyDeletedCook_ReturnsFalse()
    {
        var db = BuildDb("soft-delete-already-deleted");
        var (recipe, user) = await SeedMinimalRecipeAsync(db);

        var cookInstance = new CookInstance
        {
            RecipeId = recipe.Id,
            UserId = user.Id,
            StartedAt = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow.AddMinutes(-5)  // already soft-deleted
        };
        db.CookInstances.Add(cookInstance);
        await db.SaveChangesAsync();

        var service = new CookInstanceService(db);
        var result = await service.SoftDeleteAsync(cookInstance.Id);

        Assert.False(result);
    }

    // -----------------------------------------------------------------------
    // PatchIngredientAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task PatchIngredient_UpdatesChecked()
    {
        var db = BuildDb("patch-ingredient-checked");
        var (recipe, user) = await SeedMinimalRecipeAsync(db);

        var cookInstance = new CookInstance
        {
            RecipeId = recipe.Id,
            UserId = user.Id,
            StartedAt = DateTime.UtcNow
        };
        db.CookInstances.Add(cookInstance);
        await db.SaveChangesAsync();

        var ingredient = new CookInstanceIngredient
        {
            CookInstanceId = cookInstance.Id,
            IngredientId = db.Ingredients.First().Id,
            Amount = 100m,
            Checked = false,
            IsLimiter = false
        };
        db.CookInstanceIngredients.Add(ingredient);
        await db.SaveChangesAsync();

        var service = new CookInstanceService(db);
        var result = await service.PatchIngredientAsync(
            cookInstance.Id,
            ingredient.Id,
            new PatchCookInstanceIngredientDto { Checked = true });

        Assert.True(result);

        var raw = await db.CookInstanceIngredients.FindAsync(ingredient.Id);
        Assert.True(raw?.Checked);
    }

    [Fact]
    public async Task PatchIngredient_WrongCookInstance_ReturnsFalse()
    {
        var db = BuildDb("patch-ingredient-wrong-cook");
        var (recipe, user) = await SeedMinimalRecipeAsync(db);

        var cookInstance = new CookInstance
        {
            RecipeId = recipe.Id,
            UserId = user.Id,
            StartedAt = DateTime.UtcNow
        };
        db.CookInstances.Add(cookInstance);
        await db.SaveChangesAsync();

        var ingredient = new CookInstanceIngredient
        {
            CookInstanceId = cookInstance.Id,
            IngredientId = db.Ingredients.First().Id,
            Amount = 100m,
            Checked = false,
            IsLimiter = false
        };
        db.CookInstanceIngredients.Add(ingredient);
        await db.SaveChangesAsync();

        var service = new CookInstanceService(db);

        // Use wrong cook instance ID
        var result = await service.PatchIngredientAsync(
            99999,
            ingredient.Id,
            new PatchCookInstanceIngredientDto { Checked = true });

        Assert.False(result);
    }
}
