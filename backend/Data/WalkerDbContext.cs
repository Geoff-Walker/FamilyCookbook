using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data.Entities;

namespace WalkerFcb.Api.Data;

/// <summary>
/// EF Core database context for the WalkerFCB application.
/// Full entity configurations are completed in WAL-27; this file carries
/// the model configuration needed to generate the InitialSchema migration (WAL-25).
/// </summary>
public class WalkerDbContext : DbContext
{
    public WalkerDbContext(DbContextOptions<WalkerDbContext> options) : base(options)
    {
    }

    // ---------------------------------------------------------------------------
    // DbSets
    // ---------------------------------------------------------------------------
    public DbSet<User> Users => Set<User>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeStage> RecipeStages => Set<RecipeStage>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<TagCategory> TagCategories => Set<TagCategory>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<RecipeTag> RecipeTags => Set<RecipeTag>();
    public DbSet<RecipeReview> RecipeReviews => Set<RecipeReview>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // -----------------------------------------------------------------------
        // users
        // -----------------------------------------------------------------------
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).UseIdentityByDefaultColumn();
            e.Property(u => u.Name).IsRequired();
        });

        // -----------------------------------------------------------------------
        // recipes
        // -----------------------------------------------------------------------
        modelBuilder.Entity<Recipe>(e =>
        {
            e.ToTable("recipes");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).UseIdentityByDefaultColumn();
            e.Property(r => r.Title).IsRequired();
            e.Property(r => r.Embedding).HasColumnType("vector(1536)");
            e.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
            e.Property(r => r.UpdatedAt).HasDefaultValueSql("now()");
        });

        // -----------------------------------------------------------------------
        // recipe_stages
        // -----------------------------------------------------------------------
        modelBuilder.Entity<RecipeStage>(e =>
        {
            e.ToTable("recipe_stages");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).UseIdentityByDefaultColumn();
            e.Property(s => s.Name).IsRequired();

            // recipe_id -> recipes CASCADE
            e.HasOne(s => s.Recipe)
             .WithMany(r => r.Stages)
             .HasForeignKey(s => s.RecipeId)
             .OnDelete(DeleteBehavior.Cascade);

            // sub_recipe_id -> recipes SET NULL (nullable)
            e.HasOne(s => s.SubRecipe)
             .WithMany()
             .HasForeignKey(s => s.SubRecipeId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // -----------------------------------------------------------------------
        // recipe_steps
        // -----------------------------------------------------------------------
        modelBuilder.Entity<RecipeStep>(e =>
        {
            e.ToTable("recipe_steps");
            e.HasKey(s => s.Id);
            e.Property(s => s.Id).UseIdentityByDefaultColumn();
            e.Property(s => s.Instruction).IsRequired();

            // stage_id -> recipe_stages CASCADE
            e.HasOne(s => s.Stage)
             .WithMany(rs => rs.Steps)
             .HasForeignKey(s => s.StageId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // -----------------------------------------------------------------------
        // ingredients
        // -----------------------------------------------------------------------
        modelBuilder.Entity<Ingredient>(e =>
        {
            e.ToTable("ingredients");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).UseIdentityByDefaultColumn();
            e.Property(i => i.Name).IsRequired();
            // Case-insensitive unique index created via raw SQL below (lower(name))
        });

        // -----------------------------------------------------------------------
        // units
        // -----------------------------------------------------------------------
        modelBuilder.Entity<Unit>(e =>
        {
            e.ToTable("units");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).UseIdentityByDefaultColumn();
            e.Property(u => u.Name).IsRequired();
            e.Property(u => u.UnitType).IsRequired();
        });

        // -----------------------------------------------------------------------
        // recipe_ingredients
        // -----------------------------------------------------------------------
        modelBuilder.Entity<RecipeIngredient>(e =>
        {
            e.ToTable("recipe_ingredients");
            e.HasKey(ri => ri.Id);
            e.Property(ri => ri.Id).UseIdentityByDefaultColumn();

            // recipe_id -> recipes CASCADE
            e.HasOne(ri => ri.Recipe)
             .WithMany(r => r.Ingredients)
             .HasForeignKey(ri => ri.RecipeId)
             .OnDelete(DeleteBehavior.Cascade);

            // stage_id -> recipe_stages SET NULL (nullable)
            e.HasOne(ri => ri.Stage)
             .WithMany(s => s.Ingredients)
             .HasForeignKey(ri => ri.StageId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);

            // ingredient_id -> ingredients RESTRICT
            e.HasOne(ri => ri.Ingredient)
             .WithMany(i => i.RecipeIngredients)
             .HasForeignKey(ri => ri.IngredientId)
             .OnDelete(DeleteBehavior.Restrict);

            // unit_id -> units SET NULL (nullable)
            e.HasOne(ri => ri.Unit)
             .WithMany(u => u.RecipeIngredients)
             .HasForeignKey(ri => ri.UnitId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // -----------------------------------------------------------------------
        // tag_categories
        // -----------------------------------------------------------------------
        modelBuilder.Entity<TagCategory>(e =>
        {
            e.ToTable("tag_categories");
            e.HasKey(tc => tc.Id);
            e.Property(tc => tc.Id).UseIdentityByDefaultColumn();
            e.Property(tc => tc.Name).IsRequired();
            e.HasIndex(tc => tc.Name).IsUnique();
        });

        // -----------------------------------------------------------------------
        // tags
        // -----------------------------------------------------------------------
        modelBuilder.Entity<Tag>(e =>
        {
            e.ToTable("tags");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).UseIdentityByDefaultColumn();
            e.Property(t => t.Name).IsRequired();
            e.Property(t => t.Slug).IsRequired();
            e.HasIndex(t => t.Slug).IsUnique();

            // category_id -> tag_categories CASCADE
            e.HasOne(t => t.Category)
             .WithMany(tc => tc.Tags)
             .HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // -----------------------------------------------------------------------
        // recipe_tags — composite PK join table
        // -----------------------------------------------------------------------
        modelBuilder.Entity<RecipeTag>(e =>
        {
            e.ToTable("recipe_tags");
            e.HasKey(rt => new { rt.RecipeId, rt.TagId });

            e.HasOne(rt => rt.Recipe)
             .WithMany(r => r.RecipeTags)
             .HasForeignKey(rt => rt.RecipeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(rt => rt.Tag)
             .WithMany(t => t.RecipeTags)
             .HasForeignKey(rt => rt.TagId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // -----------------------------------------------------------------------
        // recipe_reviews
        // -----------------------------------------------------------------------
        modelBuilder.Entity<RecipeReview>(e =>
        {
            e.ToTable("recipe_reviews");
            e.HasKey(rr => rr.Id);
            e.Property(rr => rr.Id).UseIdentityByDefaultColumn();
            e.Property(rr => rr.CreatedAt).HasDefaultValueSql("now()");

            // rating CHECK 1-5 is enforced via raw SQL in the migration
            e.Property(rr => rr.Rating).IsRequired();

            e.HasOne(rr => rr.Recipe)
             .WithMany(r => r.Reviews)
             .HasForeignKey(rr => rr.RecipeId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(rr => rr.User)
             .WithMany(u => u.Reviews)
             .HasForeignKey(rr => rr.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
