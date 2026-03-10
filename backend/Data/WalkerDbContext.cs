using Microsoft.EntityFrameworkCore;
using WalkerFcb.Api.Data.Entities;

namespace WalkerFcb.Api.Data;

/// <summary>
/// EF Core database context for the WalkerFCB application.
/// All entity configurations use Fluent API only — no data annotations on entity classes.
/// Table and column names are controlled by <c>UseSnakeCaseNamingConvention()</c> applied
/// at registration time in <c>Program.cs</c> and in <see cref="WalkerDbContextFactory"/>.
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

            e.HasData(
                new User { Id = 1, Name = "Geoff" },
                new User { Id = 2, Name = "Helen" }
            );
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

            // Soft delete — all queries automatically exclude deleted recipes
            e.HasQueryFilter(r => r.DeletedAt == null);
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

            e.HasData(
                // mass
                new Unit { Id =  1, Name = "gram",       Abbreviation = "g",          UnitType = "mass"      },
                new Unit { Id =  2, Name = "kilogram",   Abbreviation = "kg",         UnitType = "mass"      },
                new Unit { Id =  3, Name = "ounce",      Abbreviation = "oz",         UnitType = "mass"      },
                new Unit { Id =  4, Name = "pound",      Abbreviation = "lb",         UnitType = "mass"      },
                // volume
                new Unit { Id =  5, Name = "millilitre", Abbreviation = "ml",         UnitType = "volume"    },
                new Unit { Id =  6, Name = "litre",      Abbreviation = "l",          UnitType = "volume"    },
                new Unit { Id =  7, Name = "teaspoon",   Abbreviation = "tsp",        UnitType = "volume"    },
                new Unit { Id =  8, Name = "tablespoon", Abbreviation = "tbsp",       UnitType = "volume"    },
                new Unit { Id =  9, Name = "cup",        Abbreviation = "cup",        UnitType = "volume"    },
                // arbitrary
                new Unit { Id = 10, Name = "pinch",      Abbreviation = "pinch",      UnitType = "arbitrary" },
                new Unit { Id = 11, Name = "handful",    Abbreviation = "handful",    UnitType = "arbitrary" },
                new Unit { Id = 12, Name = "to taste",   Abbreviation = "to taste",   UnitType = "arbitrary" },
                // count
                new Unit { Id = 13, Name = "whole",      Abbreviation = "x",          UnitType = "count"     }
            );
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

            e.HasData(
                new TagCategory { Id = 1, Name = "Cuisine"  },
                new TagCategory { Id = 2, Name = "Dietary"  },
                new TagCategory { Id = 3, Name = "Occasion" },
                new TagCategory { Id = 4, Name = "Season"   }
            );
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

            e.HasData(
                // Cuisine (category_id = 1)
                new Tag { Id =  1, CategoryId = 1, Name = "Italian",          Slug = "italian"           },
                new Tag { Id =  2, CategoryId = 1, Name = "Chinese",          Slug = "chinese"           },
                new Tag { Id =  3, CategoryId = 1, Name = "Indian",           Slug = "indian"            },
                new Tag { Id =  4, CategoryId = 1, Name = "Mexican",          Slug = "mexican"           },
                new Tag { Id =  5, CategoryId = 1, Name = "French",           Slug = "french"            },
                new Tag { Id =  6, CategoryId = 1, Name = "Thai",             Slug = "thai"              },
                new Tag { Id =  7, CategoryId = 1, Name = "Japanese",         Slug = "japanese"          },
                new Tag { Id =  8, CategoryId = 1, Name = "British",          Slug = "british"           },
                // Dietary (category_id = 2)
                new Tag { Id =  9, CategoryId = 2, Name = "Vegetarian",       Slug = "vegetarian"        },
                new Tag { Id = 10, CategoryId = 2, Name = "Vegan",            Slug = "vegan"             },
                new Tag { Id = 11, CategoryId = 2, Name = "Gluten-Free",      Slug = "gluten-free"       },
                new Tag { Id = 12, CategoryId = 2, Name = "Dairy-Free",       Slug = "dairy-free"        },
                new Tag { Id = 13, CategoryId = 2, Name = "Nut-Free",         Slug = "nut-free"          },
                // Occasion (category_id = 3)
                new Tag { Id = 14, CategoryId = 3, Name = "Weeknight",        Slug = "weeknight"         },
                new Tag { Id = 15, CategoryId = 3, Name = "Weekend",          Slug = "weekend"           },
                new Tag { Id = 16, CategoryId = 3, Name = "Special Occasion", Slug = "special-occasion"  },
                new Tag { Id = 17, CategoryId = 3, Name = "Batch Cook",       Slug = "batch-cook"        },
                new Tag { Id = 18, CategoryId = 3, Name = "Quick",            Slug = "quick"             },
                // Season (category_id = 4)
                new Tag { Id = 19, CategoryId = 4, Name = "Spring",           Slug = "spring"            },
                new Tag { Id = 20, CategoryId = 4, Name = "Summer",           Slug = "summer"            },
                new Tag { Id = 21, CategoryId = 4, Name = "Autumn",           Slug = "autumn"            },
                new Tag { Id = 22, CategoryId = 4, Name = "Winter",           Slug = "winter"            }
            );
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
