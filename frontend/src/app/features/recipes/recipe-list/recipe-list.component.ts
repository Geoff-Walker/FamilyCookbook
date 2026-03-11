import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { RecipeSummaryDto } from '../../../core/models/recipe.models';
import { RecipeGridComponent } from '../recipe-grid/recipe-grid.component';
import { LoadingSkeletonComponent } from '../loading-skeleton/loading-skeleton.component';
import { SemanticSearchComponent, SearchResult } from '../../../shared/components/semantic-search/semantic-search.component';

type ViewState = 'loading' | 'populated' | 'empty' | 'error';

@Component({
  selector: 'app-recipe-list',
  standalone: true,
  imports: [CommonModule, RouterLink, RecipeGridComponent, LoadingSkeletonComponent, SemanticSearchComponent],
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss'
})
export class RecipeListComponent implements OnInit {
  viewState: ViewState = 'loading';
  recipes: RecipeSummaryDto[] = [];

  // The full unfiltered list fetched on init — preserved so clear restores it (AC7)
  private baseRecipes: RecipeSummaryDto[] = [];

  // Search state flags
  isSearchActive = false;
  searchQuery = '';
  searchError = false; // AC6: shown as a banner, list preserved

  constructor(
    private readonly recipeApi: RecipeApiService,
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
  // Search integration (AC2–AC9)
  // -------------------------------------------------------------------------

  onSearchStateChange(result: SearchResult): void {
    // Always clear search error banner when a new state arrives
    this.searchError = false;

    switch (result.state) {
      case 'idle':
        // AC7 / AC8: clear or short query — restore full list
        this.isSearchActive = false;
        this.searchQuery = '';
        this.recipes = this.baseRecipes;
        this.viewState = this.baseRecipes.length > 0 ? 'populated' : 'empty';
        break;

      case 'loading':
        // AC3: show loading state; search is now active
        this.isSearchActive = true;
        this.searchQuery = result.query;
        this.viewState = 'loading';
        break;

      case 'results':
        // AC4: render search results using the same card layout
        this.isSearchActive = true;
        this.searchQuery = result.query;
        this.recipes = result.recipes;
        this.viewState = 'populated';
        break;

      case 'empty':
        // AC5: no recipes found for query
        this.isSearchActive = true;
        this.searchQuery = result.query;
        this.recipes = [];
        this.viewState = 'empty';
        break;

      case 'error':
        // AC6: search failed — show error banner; last known list state preserved
        this.searchError = true;
        // Do NOT change viewState or recipes — the grid stays as-is
        break;
    }
  }
}
