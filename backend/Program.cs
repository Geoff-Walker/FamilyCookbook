using Microsoft.EntityFrameworkCore;
using Npgsql;
using WalkerFcb.Api.Data;
using WalkerFcb.Api.Data.Repositories;
using WalkerFcb.Api.Endpoints;
using WalkerFcb.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Connection string — required; app fails at startup if missing
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. " +
        "Set the environment variable ConnectionStrings__DefaultConnection or add it to appsettings.json.");
}

// ---------------------------------------------------------------------------
// Database — Npgsql EF Core with pgvector
// ---------------------------------------------------------------------------
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<WalkerDbContext>(options =>
    options.UseNpgsql(dataSource, npgsqlOptions =>
        npgsqlOptions.UseVector())
           .UseSnakeCaseNamingConvention());

// ---------------------------------------------------------------------------
// Repositories
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddScoped<RecipeEmbeddingService>();
builder.Services.AddScoped<RecipeService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<ImageUploadService>();
builder.Services.AddHttpClient<ImageGenerationService>();
builder.Services.AddScoped<ImageGenerationService>();
builder.Services.AddHttpClient<ImageIdealiseService>();
builder.Services.AddScoped<ImageIdealiseService>();
builder.Services.AddScoped<CookInstanceService>();
builder.Services.AddScoped<MealPlanSlotService>();
builder.Services.AddScoped<RecipeSuggestionService>();

// ---------------------------------------------------------------------------
// CORS
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    }
    else
    {
        var allowedOrigins = builder.Configuration["AllowedOrigins"]
            ?? string.Empty;

        var origins = allowedOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    }
});

// ---------------------------------------------------------------------------
// OpenAI — key is optional at startup; absent key causes search endpoint to
// return HTTP 503 rather than preventing the app from starting.
// Set environment variable OpenAI__ApiKey or add to appsettings.json.
// ---------------------------------------------------------------------------
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
if (!string.IsNullOrWhiteSpace(openAiApiKey))
{
    // Key present — register a real client; the raw value is never logged.
    builder.Services.AddSingleton(new OpenAI.OpenAIClient(openAiApiKey));
}
// Key absent — no client registered; SearchService receives null via DI and
// returns HTTP 503 from the search endpoint rather than crashing the app.

// ---------------------------------------------------------------------------
// Swagger / OpenAPI — Development only
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "WalkerFCB API",
            Version = "v1",
            Description = "Family Cookbook API"
        });
    });
}

// ---------------------------------------------------------------------------
// Build
// ---------------------------------------------------------------------------
var app = builder.Build();

// ---------------------------------------------------------------------------
// Auto-migrate on startup (production safety net)
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<WalkerDbContext>().Database.Migrate();

// ---------------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------------
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WalkerFCB API v1");
        options.RoutePrefix = "swagger";
    });
}

// ---------------------------------------------------------------------------
// Endpoints
// ---------------------------------------------------------------------------

// Health check — no database dependency; returns immediately
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
   .WithTags("Health")
   .WithSummary("Health check");

// Feature endpoints
app.MapRecipeEndpoints();
app.MapReferenceDataEndpoints();
app.MapReviewEndpoints();
app.MapSearchEndpoints();
app.MapCookInstanceEndpoints();
app.MapMealPlanSlotEndpoints();
app.MapRecipeSuggestionEndpoints();

app.Run();
