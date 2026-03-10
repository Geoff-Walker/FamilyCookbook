using Microsoft.EntityFrameworkCore;
using Npgsql;
using WalkerFcb.Api.Data;

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
// OpenAI — key read from config; validated at startup, never logged
// ---------------------------------------------------------------------------
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
if (string.IsNullOrWhiteSpace(openAiApiKey))
{
    throw new InvalidOperationException(
        "OpenAI:ApiKey is not configured. " +
        "Set the environment variable OpenAI__ApiKey or add it to appsettings.json.");
}

// Register as a named configuration entry so services can inject it via IConfiguration.
// The raw key value is intentionally not logged anywhere.
builder.Services.AddSingleton(_ => new OpenAI.OpenAIClient(openAiApiKey));

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

// Feature endpoints are registered in Endpoints/ (WAL-17 onwards)

app.Run();
