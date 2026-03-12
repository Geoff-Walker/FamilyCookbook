using Microsoft.EntityFrameworkCore;
using Pgvector;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.Data.Entities;
using WalkerFcb.Api.Data.Repositories;
using WalkerFcb.Api.DTOs;

namespace WalkerFcb.Api.Services;

/// <summary>
/// Business logic layer for recipe CRUD operations.
/// Orchestrates the repository, ingredient upsert, and embedding pipeline.
/// </summary>
public class RecipeService
{
    private readonly IRecipeRepository _repository;
    private readonly WalkerDbContext _db;
    private readonly RecipeEmbeddingService _embeddingService;

    public RecipeService(
        IRecipeRepository repository,
        WalkerDbContext db,
        RecipeEmbeddingService embeddingService)
    {
        _repository = repository;
        _db = db;
        _embeddingService = embeddingService;
    }

    // -----------------------------------------------------------------------
    // GET ALL
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns all non-deleted recipes as summary DTOs.
    /// </summary>
    public async Task<List<RecipeSummaryDto>> GetAllAsync()
    {
        var summaries = await _repository.GetAllAsync();

        return summaries.Select(r => new RecipeSummaryDto
        {
            Id = r.Id,
            Title = r.Title,
            PrepTimeMinutes = r.PrepTimeMinutes,
            CookTimeMinutes = r.CookTimeMinutes,
            ImageUrl = r.ImageUrl,
            Tags = r.Tags.Select(t => new RecipeSummaryTagDto
            {
                Id = t.TagId,
                Name = t.TagName,
                CategoryName = t.CategoryName
            }).ToList(),
            Ratings = []
        }).ToList();
    }

    // -----------------------------------------------------------------------
    // GET BY ID
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the full recipe detail DTO, or null if not found or soft-deleted.
    /// </summary>
    public async Task<RecipeDetailDto?> GetByIdAsync(int id)
    {
        var recipe = await _repository.GetByIdAsync(id);
        return recipe == null ? null : MapToDetailDto(recipe);
    }

    // -----------------------------------------------------------------------
    // CREATE
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a new recipe from the request DTO.
    /// Returns the persisted recipe as a detail DTO plus an embedding failure flag.
    /// </summary>
    public async Task<(RecipeDetailDto Dto, bool EmbeddingFailed)> CreateAsync(CreateRecipeDto request)
    {
        var recipe = new Recipe
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Source = request.Source?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            Servings = request.Servings,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await AttachTagsAsync(recipe, request.TagIds);
        await AttachStagesFromCreateAsync(recipe, request.Stages);

        var created = await _repository.CreateAsync(recipe);

        // Re-load with full includes before embedding so navigation properties are populated
        var full = await _repository.GetByIdAsync(created.Id)
                   ?? throw new InvalidOperationException($"Recipe {created.Id} not found after create.");

        var embeddingFailed = await TryApplyEmbeddingAsync(full);

        return (MapToDetailDto(full), embeddingFailed);
    }

    // -----------------------------------------------------------------------
    // UPDATE
    // -----------------------------------------------------------------------

    /// <summary>
    /// Fully replaces a recipe's scalar fields and child collections.
    /// Returns null if the recipe is not found or soft-deleted.
    /// </summary>
    public async Task<(RecipeDetailDto? Dto, bool EmbeddingFailed)> UpdateAsync(int id, UpdateRecipeDto request)
    {
        var recipe = await _repository.GetByIdAsync(id);
        if (recipe == null)
            return (null, false);

        recipe.Title = request.Title.Trim();
        recipe.Description = request.Description?.Trim();
        recipe.Source = request.Source?.Trim();
        recipe.ImageUrl = request.ImageUrl?.Trim();
        recipe.PrepTimeMinutes = request.PrepTimeMinutes;
        recipe.CookTimeMinutes = request.CookTimeMinutes;
        recipe.Servings = request.Servings;
        recipe.UpdatedAt = DateTime.UtcNow;

        // Replace tags
        recipe.RecipeTags.Clear();
        await AttachTagsAsync(recipe, request.TagIds);

        // Replace stages — cascade-delete removes old stages/steps/ingredients
        recipe.Stages.Clear();
        // Also clear stage-scoped ingredients at recipe level
        var stageIngredients = recipe.Ingredients.Where(i => i.StageId != null).ToList();
        foreach (var si in stageIngredients)
            recipe.Ingredients.Remove(si);

        await AttachStagesFromUpdateAsync(recipe, request.Stages);

        await _repository.UpdateAsync(recipe);

        // Re-load with full includes before embedding so navigation properties are populated
        var full = await _repository.GetByIdAsync(recipe.Id)
                   ?? throw new InvalidOperationException($"Recipe {recipe.Id} not found after update.");

        var embeddingFailed = await TryApplyEmbeddingAsync(full);

        return (MapToDetailDto(full), embeddingFailed);
    }

    // -----------------------------------------------------------------------
    // SOFT DELETE
    // -----------------------------------------------------------------------

    /// <summary>
    /// Soft-deletes a recipe. Returns false if the recipe was not found.
    /// </summary>
    public async Task<bool> SoftDeleteAsync(int id)
    {
        try
        {
            await _repository.SoftDeleteAsync(id);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private async Task AttachTagsAsync(Recipe recipe, List<int> tagIds)
    {
        foreach (var tagId in tagIds.Distinct())
        {
            recipe.RecipeTags.Add(new RecipeTag
            {
                Recipe = recipe,
                TagId = tagId
            });
        }
        await Task.CompletedTask;
    }

    private async Task AttachStagesFromCreateAsync(Recipe recipe, List<CreateRecipeStageDto> stageDtos)
    {
        for (var stageIndex = 0; stageIndex < stageDtos.Count; stageIndex++)
        {
            var stageDto = stageDtos[stageIndex];
            var stage = new RecipeStage
            {
                Name = stageDto.Name.Trim(),
                Description = stageDto.Description?.Trim(),
                SortOrder = stageIndex,
                Recipe = recipe
            };

            for (var stepIndex = 0; stepIndex < stageDto.Steps.Count; stepIndex++)
            {
                stage.Steps.Add(new RecipeStep
                {
                    Instruction = stageDto.Steps[stepIndex].Instruction.Trim(),
                    SortOrder = stepIndex,
                    Stage = stage
                });
            }

            for (var ingIndex = 0; ingIndex < stageDto.Ingredients.Count; ingIndex++)
            {
                var ingDto = stageDto.Ingredients[ingIndex];
                var ingredient = await UpsertIngredientAsync(ingDto.IngredientName);
                stage.Ingredients.Add(new RecipeIngredient
                {
                    Recipe = recipe,
                    Stage = stage,
                    Ingredient = ingredient,
                    Amount = ingDto.Amount?.Trim(),
                    UnitId = ingDto.UnitId,
                    Notes = ingDto.Notes?.Trim(),
                    SortOrder = ingIndex
                });
            }

            recipe.Stages.Add(stage);
        }
    }

    private async Task AttachStagesFromUpdateAsync(Recipe recipe, List<UpdateRecipeStageDto> stageDtos)
    {
        for (var stageIndex = 0; stageIndex < stageDtos.Count; stageIndex++)
        {
            var stageDto = stageDtos[stageIndex];
            var stage = new RecipeStage
            {
                Name = stageDto.Name.Trim(),
                Description = stageDto.Description?.Trim(),
                SortOrder = stageIndex,
                Recipe = recipe
            };

            for (var stepIndex = 0; stepIndex < stageDto.Steps.Count; stepIndex++)
            {
                stage.Steps.Add(new RecipeStep
                {
                    Instruction = stageDto.Steps[stepIndex].Instruction.Trim(),
                    SortOrder = stepIndex,
                    Stage = stage
                });
            }

            for (var ingIndex = 0; ingIndex < stageDto.Ingredients.Count; ingIndex++)
            {
                var ingDto = stageDto.Ingredients[ingIndex];
                var ingredient = await UpsertIngredientAsync(ingDto.IngredientName);
                stage.Ingredients.Add(new RecipeIngredient
                {
                    Recipe = recipe,
                    Stage = stage,
                    Ingredient = ingredient,
                    Amount = ingDto.Amount?.Trim(),
                    UnitId = ingDto.UnitId,
                    Notes = ingDto.Notes?.Trim(),
                    SortOrder = ingIndex
                });
            }

            recipe.Stages.Add(stage);
        }
    }

    /// <summary>
    /// Finds an ingredient by name (case-insensitive) or creates it if not found.
    /// </summary>
    private async Task<Ingredient> UpsertIngredientAsync(string name)
    {
        var normalised = name.Trim();
        var existing = await _db.Ingredients
            .FirstOrDefaultAsync(i => i.Name.ToLower() == normalised.ToLower());

        if (existing != null)
            return existing;

        var created = new Ingredient { Name = normalised };
        _db.Ingredients.Add(created);
        await _db.SaveChangesAsync();
        return created;
    }

    /// <summary>
    /// Calls the embedding service after persist. If it throws, the recipe is already
    /// saved and the caller receives an embeddingFailed flag to set the response header.
    /// </summary>
    private async Task<bool> TryApplyEmbeddingAsync(Recipe recipe)
    {
        try
        {
            var (summary, embedding) = await _embeddingService.GenerateAsync(recipe);
            if (summary != null || embedding != null)
            {
                recipe.Summary = summary;
                recipe.Embedding = embedding != null ? new Vector(embedding) : null;
                await _db.SaveChangesAsync();
            }
            return false;
        }
        catch
        {
            return true;
        }
    }

    // -----------------------------------------------------------------------
    // Projection
    // -----------------------------------------------------------------------

    private static RecipeDetailDto MapToDetailDto(Recipe recipe)
    {
        return new RecipeDetailDto
        {
            Id = recipe.Id,
            Title = recipe.Title,
            Description = recipe.Description,
            Source = recipe.Source,
            ImageUrl = recipe.ImageUrl,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookTimeMinutes = recipe.CookTimeMinutes,
            Servings = recipe.Servings,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt,
            Stages = recipe.Stages
                .OrderBy(s => s.SortOrder)
                .Select(s => new RecipeDetailStageDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    SortOrder = s.SortOrder,
                    Steps = s.Steps
                        .OrderBy(st => st.SortOrder)
                        .Select(st => new RecipeDetailStepDto
                        {
                            Id = st.Id,
                            Instruction = st.Instruction,
                            SortOrder = st.SortOrder
                        }).ToList(),
                    Ingredients = s.Ingredients
                        .OrderBy(i => i.SortOrder)
                        .Select(i => new RecipeDetailIngredientDto
                        {
                            Id = i.Id,
                            IngredientId = i.IngredientId,
                            IngredientName = i.Ingredient.Name,
                            Amount = i.Amount,
                            UnitId = i.UnitId,
                            UnitName = i.Unit?.Name,
                            UnitAbbreviation = i.Unit?.Abbreviation,
                            Notes = i.Notes,
                            SortOrder = i.SortOrder
                        }).ToList()
                }).ToList(),
            Tags = recipe.RecipeTags
                .OrderBy(rt => rt.Tag.Category.Name)
                .ThenBy(rt => rt.Tag.Name)
                .Select(rt => new RecipeDetailTagDto
                {
                    Id = rt.TagId,
                    Name = rt.Tag.Name,
                    CategoryName = rt.Tag.Category.Name
                }).ToList(),
            Reviews = recipe.Reviews
                .OrderByDescending(rv => rv.CreatedAt)
                .Select(rv => new RecipeDetailReviewDto
                {
                    Id = rv.Id,
                    UserId = rv.UserId,
                    UserName = rv.User.Name,
                    Rating = rv.Rating,
                    Notes = rv.Notes,
                    MadeOn = rv.MadeOn,
                    CreatedAt = rv.CreatedAt
                }).ToList()
        };
    }
}
