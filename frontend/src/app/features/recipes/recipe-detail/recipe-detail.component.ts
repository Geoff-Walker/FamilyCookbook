import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { RecipeDetailDto, RecipeDetailReviewDto, RecipeDetailTagDto, RecipeDetailStageDto } from '../../../core/models/recipe.models';
import { RecipeHeroComponent } from '../recipe-hero/recipe-hero.component';
import { IngredientListComponent } from '../ingredient-list/ingredient-list.component';
import { MethodStepsComponent } from '../method-steps/method-steps.component';
import { RatingReviewComponent } from '../rating-review/rating-review.component';
import { HeaderStateService } from '../../../core/services/header-state.service';

type ViewState = 'loading' | 'loaded' | 'notFound' | 'error';

@Component({
  selector: 'app-recipe-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, RecipeHeroComponent, IngredientListComponent, MethodStepsComponent, RatingReviewComponent],
  templateUrl: './recipe-detail.component.html',
  styleUrl: './recipe-detail.component.scss'
})
export class RecipeDetailComponent implements OnInit, OnDestroy {
  viewState: ViewState = 'loading';
  recipe: RecipeDetailDto | null = null;
  recipeId!: number;

  showDeleteConfirm = false;
  isDeleting = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly recipeApi: RecipeApiService,
    private readonly headerState: HeaderStateService,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.recipeId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  ngOnDestroy(): void {
    this.headerState.setPageTitle(null);
  }

  load(): void {
    this.viewState = 'loading';
    this.recipeApi.getRecipe(this.recipeId).subscribe({
      next: (data) => {
        this.recipe = data;
        this.viewState = 'loaded';
        this.headerState.setPageTitle(data.title);
      },
      error: (err: HttpErrorResponse) => {
        this.viewState = err.status === 404 ? 'notFound' : 'error';
        this.headerState.setPageTitle(null);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  goEdit(): void {
    this.router.navigate(['/recipes', this.recipeId, 'edit']);
  }

  promptDelete(): void {
    this.showDeleteConfirm = true;
  }

  cancelDelete(): void {
    this.showDeleteConfirm = false;
  }

  confirmDelete(): void {
    if (this.isDeleting) return;
    this.isDeleting = true;
    this.recipeApi.deleteRecipe(this.recipeId).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: () => {
        this.isDeleting = false;
        this.showDeleteConfirm = false;
        this.snackBar.open('Could not delete recipe. Please try again.', 'Dismiss', { duration: 5000 });
      }
    });
  }

  get isMultiStage(): boolean {
    return (this.recipe?.stages.length ?? 0) > 1;
  }

  get tagsByCategory(): Record<string, RecipeDetailTagDto[]> {
    if (!this.recipe) return {};
    return this.recipe.tags.reduce((acc, tag) => {
      (acc[tag.categoryName] ??= []).push(tag);
      return acc;
    }, {} as Record<string, RecipeDetailTagDto[]>);
  }

  get tagCategories(): string[] {
    return Object.keys(this.tagsByCategory);
  }

  get reviewsByUser(): { userName: string; reviews: RecipeDetailReviewDto[] }[] {
    if (!this.recipe) return [];
    const map = new Map<string, RecipeDetailReviewDto[]>();
    for (const review of this.recipe.reviews) {
      const list = map.get(review.userName) ?? [];
      list.push(review);
      map.set(review.userName, list);
    }
    map.forEach(list =>
      list.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    );
    return Array.from(map.entries()).map(([userName, reviews]) => ({ userName, reviews }));
  }

  formatTime(minutes: number | null): string {
    if (!minutes) return '';
    if (minutes < 60) return `${minutes}m`;
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return m > 0 ? `${h}h ${m}m` : `${h}h`;
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  stepsStartNumber(stage: RecipeDetailStageDto): number {
    if (!this.recipe || !this.isMultiStage) return 1;
    let total = 1;
    for (const s of this.recipe.stages) {
      if (s.id === stage.id) break;
      total += s.steps.length;
    }
    return total;
  }

  starDisplay(rating: number): string {
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  }
}
