import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { RecipeSummaryDto } from '../../../core/models/recipe.models';
import { RecipeGridComponent } from '../recipe-grid/recipe-grid.component';
import { LoadingSkeletonComponent } from '../loading-skeleton/loading-skeleton.component';
import { SemanticSearchComponent, SearchResult } from '../../../shared/components/semantic-search/semantic-search.component';
import {
  IngredientFilterPanelComponent,
  IngredientFilterState
} from '../ingredient-filter-panel/ingredient-filter-panel.component';
import {
  TagFilterComponent,
  TagFilterState
} from '../tag-filter/tag-filter.component';
import { UserStateService } from '../../../core/services/user-state.service';

type ViewState = 'loading' | 'populated' | 'empty' | 'error';

@Component({
  selector: 'app-recipe-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RecipeGridComponent,
    LoadingSkeletonComponent,
    SemanticSearchComponent,
    IngredientFilterPanelComponent,
    TagFilterComponent
  ],
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss'
})
export class RecipeListComponent implements OnInit {
  viewState: ViewState = 'loading';
  recipes: RecipeSummaryDto[] = [];

  // The full unfiltered list fetched on init — preserved so clear restores it
  private baseRecipes: RecipeSummaryDto[] = [];

  // Semantic search state
  isSearchActive = false;
  searchQuery = '';
  searchError = false; // shown as a banner, list preserved on error

  // Filter error / empty state flags (shared across ingredient and tag filters)
  filterError = false;       // AC5/AC7: filter API error banner
  filterEmptyMessage = false; // AC4/AC6: no results for filter combination

  // Tag-specific empty state (AC4)
  tagFilterEmptyMessage = false;

  private activeSearch: SearchResult = { state: 'idle', recipes: [], query: '' };
  private activeFilter: IngredientFilterState = { hasFilters: false, ingredientIds: [] };
  private activeTagFilter: TagFilterState = { hasFilters: false, tagIds: [] };

  constructor(
    private readonly recipeApi: RecipeApiService,
    private readonly userState: UserStateService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.viewState = 'loading';
    this.recipeApi.getRecipes().subscribe({
      next: (data) => {
        this.baseRecipes = data;
        this.recipes = data;
        this.viewState = data.length > 0 ? 'populated' : 'empty';
      },
      error: () => {
        this.viewState = 'error';
      }
    });
  }

  navigateToAdd(): void {
    this.router.navigate(['/recipes/new']);
  }

  // -------------------------------------------------------------------------
  // Semantic search integration (WAL-11)
  // -------------------------------------------------------------------------

  onSearchStateChange(result: SearchResult): void {
    this.searchError = false;
    this.activeSearch = result;
    this.applyFilters();
  }

  // -------------------------------------------------------------------------
  // Ingredient filter integration (WAL-12)
  // -------------------------------------------------------------------------

  onFilterStateChange(state: IngredientFilterState): void {
    this.activeFilter = state;
    this.applyFilters();
  }

  // -------------------------------------------------------------------------
  // Tag filter integration (WAL-13)
  // -------------------------------------------------------------------------

  onTagFilterStateChange(state: TagFilterState): void {
    this.activeTagFilter = state;
    this.applyFilters();
  }

  // -------------------------------------------------------------------------
  // Combined filter logic (AC11 — all three filters combine correctly)
  // -------------------------------------------------------------------------

  private applyFilters(): void {
    this.filterError = false;
    this.filterEmptyMessage = false;
    this.tagFilterEmptyMessage = false;
    this.searchError = false;

    const searchActive        = this.activeSearch.state !== 'idle';
    const ingredientActive    = this.activeFilter.hasFilters;
    const tagActive           = this.activeTagFilter.hasFilters;
    const anyFilterActive     = ingredientActive || tagActive;

    // Nothing active — restore full list
    if (!searchActive && !anyFilterActive) {
      this.isSearchActive = false;
      this.searchQuery = '';
      this.recipes = this.baseRecipes;
      this.viewState = this.baseRecipes.length > 0 ? 'populated' : 'empty';
      return;
    }

    // Search in-flight — show loading
    if (this.activeSearch.state === 'loading') {
      this.isSearchActive = true;
      this.searchQuery = this.activeSearch.query;
      this.viewState = 'loading';
      return;
    }

    // Search errored — show error banner, preserve list
    if (this.activeSearch.state === 'error') {
      this.searchError = true;
      return;
    }

    const userId = this.userState.activeUserId;

    // -----------------------------------------------------------------------
    // Semantic search active + one or both static filters active (AC11)
    // -----------------------------------------------------------------------
    if (searchActive && anyFilterActive && this.activeSearch.state === 'results') {
      if (userId <= 0) return;

      this.isSearchActive = true;
      this.searchQuery = this.activeSearch.query;
      this.viewState = 'loading';

      const obs = this.recipeApi.searchWithFilters(
        this.activeSearch.query,
        userId,
        this.activeFilter.ingredientIds,
        this.activeTagFilter.tagIds
      );

      obs.subscribe({
        next: (results) => {
          this.recipes = results;
          if (results.length === 0) {
            this.filterEmptyMessage = true;
            this.viewState = 'empty';
          } else {
            this.viewState = 'populated';
          }
        },
        error: () => {
          this.filterError = true;
          this.viewState = 'populated';
        }
      });

      return;
    }

    // -----------------------------------------------------------------------
    // Static filters only (no active semantic search)
    // -----------------------------------------------------------------------
    if (anyFilterActive && !searchActive) {
      this.isSearchActive = false;
      this.searchQuery = '';
      this.viewState = 'loading';

      let obs$;

      if (ingredientActive && tagActive) {
        // Both filters active
        obs$ = this.recipeApi.filterByIngredientsAndTags(
          this.activeFilter.ingredientIds,
          this.activeTagFilter.tagIds
        );
      } else if (tagActive) {
        // Tag filter only (AC3)
        obs$ = this.recipeApi.filterByTags(this.activeTagFilter.tagIds);
      } else {
        // Ingredient filter only
        obs$ = this.recipeApi.filterRecipes(this.activeFilter.ingredientIds);
      }

      obs$.subscribe({
        next: (results) => {
          this.recipes = results;
          if (results.length === 0) {
            // AC4: tag-only empty; AC6: ingredient/combined empty
            if (tagActive && !ingredientActive) {
              this.tagFilterEmptyMessage = true;
            } else {
              this.filterEmptyMessage = true;
            }
            this.viewState = 'empty';
          } else {
            this.viewState = 'populated';
          }
        },
        error: () => {
          // AC5: tag filter error; AC7: ingredient filter error
          this.filterError = true;
          this.viewState = 'populated';
        }
      });

      return;
    }

    // -----------------------------------------------------------------------
    // Only semantic search active (no static filters)
    // -----------------------------------------------------------------------
    if (searchActive && !anyFilterActive) {
      this.isSearchActive = true;
      this.searchQuery = this.activeSearch.query;

      switch (this.activeSearch.state) {
        case 'results':
          this.recipes = this.activeSearch.recipes;
          this.viewState = 'populated';
          break;
        case 'empty':
          this.recipes = [];
          this.viewState = 'empty';
          break;
      }
    }
  }
}
