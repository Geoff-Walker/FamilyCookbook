import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { RecipeSummaryDto } from '../../../core/models/recipe.models';
import { RecipeGridComponent } from '../recipe-grid/recipe-grid.component';
import { LoadingSkeletonComponent } from '../loading-skeleton/loading-skeleton.component';

type ViewState = 'loading' | 'populated' | 'empty' | 'error';

@Component({
  selector: 'app-recipe-list',
  standalone: true,
  imports: [CommonModule, RouterLink, RecipeGridComponent, LoadingSkeletonComponent],
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss'
})
export class RecipeListComponent implements OnInit {
  viewState: ViewState = 'loading';
  recipes: RecipeSummaryDto[] = [];

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
}
