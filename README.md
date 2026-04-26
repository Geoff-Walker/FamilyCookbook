# FamilyCookbook

A personal recipe management application with AI-powered semantic search and image generation. Built as a full-stack web application and running in production on a self-hosted home server.

---

## What it does

FamilyCookbook started as a recipe box replacement and grew into something more useful. The two AI features are the ones that actually changed how it gets used day-to-day:

**Semantic search** — search by feel rather than keyword. "Something warming and autumnal" finds mushroom risotto and lentil dahl without either word appearing in the recipe title or ingredients. Powered by pgvector and OpenAI text-embedding-3-small embeddings stored alongside each recipe.

**AI image generation** — recipes without photos get an AI-generated image based on the recipe title and ingredients. Existing photos can be run through the same pipeline to produce an idealised version. Small thing, but it makes the recipe list feel like an actual cookbook.

Beyond the AI features, it covers the full lifecycle of actually using recipes rather than just storing them:

- **Meal planner** — weekly calendar view, shopping week planner, and auto-generated shopping list from the week's recipes
- **Cook instances** — log each time a recipe is cooked, with ingredient scaling, notes, and a limiter mechanism to scale the whole recipe from a single constraining ingredient
- **Cook history and versioning** — every cook is saved. Promote a particularly good cook's notes back to the main recipe.
- **The Geoff Filter** — a suggestion queue for recipes to try. Acts as a wishlist that feeds into meal planning.
- **Ratings and reviews** — per-cook rating and notes, displayed on a two-card layout per cook instance

---

## Tech stack

| Layer | Technology |
|-------|------------|
| Frontend | Angular 18, Angular Material |
| Backend | .NET 8 Web API, Entity Framework Core |
| Database | PostgreSQL 16 + pgvector |
| AI | OpenAI text-embedding-3-small (semantic search), OpenAI image generation |
| Containerisation | Docker, Docker Compose |
| CI/CD | GitHub Actions → GHCR |
| Secrets | Infisical |
| Infrastructure | Self-hosted TrueNAS SCALE, Linux |

---

## Architecture

```
Angular SPA
    │
    │  /api/* (proxied by nginx in the frontend container)
    ▼
.NET 8 Web API
    │
    ├── PostgreSQL + pgvector
    │     └── Recipe embeddings (1536-dim, text-embedding-3-small)
    │
    └── OpenAI API
          ├── Embeddings (search query → vector → similarity search)
          └── Image generation (DALL-E)
```

The frontend and backend run as separate Docker containers. The nginx configuration in the frontend container proxies `/api/` requests to the backend — no CORS configuration needed, and the whole stack is served from a single port.

Semantic search works in two steps: at recipe save time, the recipe title and ingredients are embedded and stored in pgvector. At search time, the query is embedded and a cosine similarity search finds the closest matches. No keyword matching involved.

---

## CI/CD

GitHub Actions builds and pushes Docker images to GHCR on every PR and merge:

- **On PR open/update** → builds `:staging` tags → staging environment updated for review
- **On merge to main** → builds `:latest` and `:<sha>` tags → production stack updated manually

The production stack runs on a self-hosted server. Staging is a fully isolated environment with its own Postgres volume — no shared data with production.

Secrets (OpenAI API key, Postgres password) are managed via Infisical and injected at container startup, not baked into the images.

---

## Local development

```bash
# Requires Infisical CLI and Docker
cp .env.example .env   # or use Infisical export
./start-dev.sh
```

The dev compose file mounts the source directories for hot reload on both frontend (Angular CLI) and backend (dotnet watch).

---

## Status

In production and in daily use. All planned phases complete:

- **Phase 1** — Recipe CRUD, semantic search, AI image generation, responsive UI
- **Phase 2** — Cook instances, portion scaling, cook history, versioning, promote-to-recipe
- **Phase 3** — Meal planner, shopping list, The Geoff Filter, ratings and reviews

Built as the first project through [AppFactory](https://github.com/Geoff-Walker/AppFactory-Architecture) — a multi-agent AI development pipeline.
