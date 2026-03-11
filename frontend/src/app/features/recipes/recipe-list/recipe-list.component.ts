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
    IngredientFilterPanelComponent
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

  // Ingredient filter state (AC5–AC10)
  filterError = false; // AC7: filter API error banner
  filterEmptyMessage = false; // AC6: no results for filter combination

  private activeSearch: SearchResult = { state: 'idle', recipes: [], query: '' };
  private activeFilter: IngredientFilterState = { hasFilters: false, ingredientIds: [] };

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
  // Combined filter logic (AC5, AC6, AC7, AC8, AC10)
  // -------------------------------------------------------------------------

  private applyFilters(): void {
    this.filterError = false;
    this.filterEmptyMessage = false;
    this.searchError = false;

    const searchActive = this.activeSearch.state !== 'idle';
    const filterActive = this.activeFilter.hasFilters;

    if (!searchActive && !filterActive) {
      // AC8: nothing active — restore full list
      this.isSearchActive = false;
      this.searchQuery = '';
      this.recipes = this.baseRecipes;
      this.viewState = this.baseRecipes.length > 0 ? 'populated' : 'empty';
      return;
    }

    if (this.activeSearch.state === 'loading') {
      // Search in-flight — show loading, keep filter state pending
      this.isSearchActive = true;
      this.searchQuery = this.activeSearch.query;
      this.viewState = 'loading';
      return;
    }

    if (this.activeSearch.state === 'error') {
      // AC6 (search): show error banner; list preserved
      this.searchError = true;
      return;
    }

    // AC10: Both semantic search results AND ingredient filters active
    if (searchActive && filterActive && this.activeSearch.state === 'results') {
      const userId = this.userState.activeUserId;
      if (userId <= 0) return;

      this.isSearchActive = true;
      this.searchQuery = this.activeSearch.query;
      this.viewState = 'loading';

      this.recipeApi
        .searchWithIngredients(this.activeSearch.query, userId, this.activeFilter.ingredientIds)
        .subscribe({
          next: (results) => {
            this.recipes = results;
            if (results.length === 0) {
              // AC6: combined search + filter returned empty
              this.filterEmptyMessage = true;
              this.viewState = 'empty';
            } else {
              this.viewState = 'populated';
            }
          },
          error: () => {
            // AC7: combined request failed — show error banner; list preserved
            this.filterError = true;
            this.viewState = 'populated';
          }
        });

      return;
    }

    // Only ingredient filter active (search idle or empty)
    if (filterActive && !searchActive) {
      this.isSearchActive = false;
      this.searchQuery = '';
      this.viewState = 'loading';

      this.recipeApi.filterRecipes(this.activeFilter.ingredientIds).subscribe({
        next: (results) => {
          this.recipes = results;
          if (results.length === 0) {
            // AC6: no recipes found with selected ingredients
            this.filterEmptyMessage = true;
            this.viewState = 'empty';
          } else {
            this.viewState = 'populated';
          }
        },
        error: () => {
          // AC7: filter API error — show banner; preserve previous list
          this.filterError = true;
          this.viewState = 'populated';
        }
      });

      return;
    }

    // Only semantic search active (no ingredient filter)
    if (searchActive && !filterActive) {
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
