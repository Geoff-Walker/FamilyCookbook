import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';

import { CookInstanceApiService } from '../../../core/services/cook-instance-api.service';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { UserStateService } from '../../../core/services/user-state.service';
import { HeaderStateService } from '../../../core/services/header-state.service';

import {
  CompleteCookPayload,
  CookInstanceDetailDto,
  CookInstanceReviewSummaryDto,
  CookReviewPayload
} from '../../../core/models/cook-instance.models';
import { RecipeDetailDto } from '../../../core/models/recipe.models';

import { IngredientComparisonTableComponent } from '../ingredient-comparison-table/ingredient-comparison-table.component';
import { PromoteConfirmDialogComponent } from '../promote-confirm-dialog/promote-confirm-dialog.component';

type ViewState = 'loading' | 'loaded' | 'notFound' | 'error';

/** One user's rating state for the inline review form. */
interface UserRating {
  userId: number;
  userName: string;
  rating: number;
  hoverRating: number;
  notes: string;
}

@Component({
  selector: 'app-cook-instance-view-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    IngredientComparisonTableComponent,
    PromoteConfirmDialogComponent
  ],
  templateUrl: './cook-instance-view-page.component.html',
  styleUrl: './cook-instance-view-page.component.scss'
})
export class CookInstanceViewPageComponent implements OnInit {
  viewState: ViewState = 'loading';
  cookInstance: CookInstanceDetailDto | null = null;
  recipe: RecipeDetailDto | null = null;
  cookInstanceId!: number;
  baseLabel = 'Base recipe';

  // ---------------------------------------------------------------------------
  // Promote state
  // ---------------------------------------------------------------------------

  showPromoteDialog = false;
  isPromoting = false;
  promoteSuccessMessage: string | null = null;
  promoteErrorMessage: string | null = null;
  /** Disabled after a successful promote to prevent double-promote. */
  promoteDisabled = false;

  // ---------------------------------------------------------------------------
  // Inline review form state (AC9)
  // ---------------------------------------------------------------------------

  userRatings: UserRating[] = [];
  readonly starValues = [1, 2, 3, 4, 5];
  reviewRatingError: string | null = null;
  reviewSaveError: string | null = null;
  isSavingReview = false;
  reviewSubmitted = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly cookApi: CookInstanceApiService,
    private readonly recipeApi: RecipeApiService,
    private readonly userState: UserStateService,
    private readonly headerState: HeaderStateService
  ) {}

  ngOnInit(): void {
    this.cookInstanceId = Number(this.route.snapshot.paramMap.get('id'));
    this.initUserRatings();
    this.load();
  }

  private initUserRatings(): void {
    this.userRatings = [
      { userId: 1, userName: 'Geoff', rating: 0, hoverRating: 0, notes: '' },
      { userId: 2, userName: 'Helen', rating: 0, hoverRating: 0, notes: '' }
    ];
  }

  load(): void {
    this.viewState = 'loading';

    this.cookApi.getCookInstance(this.cookInstanceId).subscribe({
      next: (cook) => {
        // AC1 guard: if in progress, redirect to the active cook page
        if (!cook.completedAt) {
          this.router.navigate(['/cook', this.cookInstanceId, 'active'], { replaceUrl: true });
          return;
        }

        this.cookInstance = cook;
        this.headerState.setPageTitle(cook.recipeTitle);

        // Fetch recipe and version history in parallel now that we have the recipeId
        forkJoin({
          recipe: this.recipeApi.getRecipe(cook.recipeId),
          versions: this.cookApi.getVersionsByRecipe(cook.recipeId)
        }).subscribe({
          next: ({ recipe, versions }) => {
            this.recipe = recipe;
            // Set baseLabel based on whether the recipe has ever been promoted
            this.baseLabel = versions.length > 0 ? 'Promoted Recipe' : 'Original Recipe';
            this.viewState = 'loaded';
          },
          error: () => {
            // Fetch failure is non-fatal — comparison table will show no base amounts,
            // baseLabel falls back to default 'Base recipe'
            this.recipe = null;
            this.viewState = 'loaded';
          }
        });
      },
      error: (err: HttpErrorResponse) => {
        this.viewState = err.status === 404 ? 'notFound' : 'error';
        this.headerState.setPageTitle(null);
      }
    });
  }

  // ---------------------------------------------------------------------------
  // Pill helpers
  // ---------------------------------------------------------------------------

  get pillLabel(): string {
    if (!this.cookInstance) return '';
    return this.cookInstance.reviews.length > 0 ? 'Completed' : 'Awaiting Review';
  }

  get pillClass(): string {
    if (!this.cookInstance) return '';
    return this.cookInstance.reviews.length > 0
      ? 'status-pill--completed'
      : 'status-pill--awaiting';
  }

  /** Look up a submitted review for a specific user, or null if not reviewed. */
  getReview(userId: number): CookInstanceReviewSummaryDto | null {
    return this.cookInstance?.reviews.find(r => r.userId === userId) ?? null;
  }

  starDisplay(rating: number): string {
    const filled = Math.floor(rating);
    const half = rating % 1 >= 0.5;
    const empty = 5 - filled - (half ? 1 : 0);
    return '★'.repeat(filled) + (half ? '½' : '') + '☆'.repeat(empty);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  // ---------------------------------------------------------------------------
  // Promote flow (AC5–AC8)
  // ---------------------------------------------------------------------------

  onPromoteClick(): void {
    this.promoteSuccessMessage = null;
    this.promoteErrorMessage = null;
    this.showPromoteDialog = true;
  }

  onPromoteConfirmed(): void {
    this.isPromoting = true;
    const userId = this.userState.activeUserId || 1;

    this.cookApi.promoteCook(this.cookInstanceId, userId).subscribe({
      next: () => {
        // Navigate to the recipe so the user immediately sees the promoted ingredients.
        this.router.navigate(['/recipes', this.cookInstance!.recipeId]);
      },
      error: () => {
        this.isPromoting = false;
        this.showPromoteDialog = false;
        this.promoteErrorMessage = 'Promote failed. No changes were made.';
        this.promoteSuccessMessage = null;
      }
    });
  }

  onPromoteCancelled(): void {
    this.showPromoteDialog = false;
  }

  // ---------------------------------------------------------------------------
  // Inline review — half-star interaction (AC9)
  // ---------------------------------------------------------------------------

  private halfStarValue(star: number, event: MouseEvent): number {
    const target = event.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const x = event.clientX - rect.left;
    return x < rect.width / 2 ? star - 0.5 : star;
  }

  onStarClick(userRating: UserRating, star: number, event: MouseEvent): void {
    userRating.rating = this.halfStarValue(star, event);
    userRating.hoverRating = 0;
    this.reviewRatingError = null;
  }

  onStarHover(userRating: UserRating, star: number, event: MouseEvent): void {
    userRating.hoverRating = this.halfStarValue(star, event);
  }

  onStarLeave(userRating: UserRating): void {
    userRating.hoverRating = 0;
  }

  leftHalfFilled(userRating: UserRating, star: number): boolean {
    const effective = userRating.hoverRating > 0 ? userRating.hoverRating : userRating.rating;
    return effective >= star - 0.5;
  }

  rightHalfFilled(userRating: UserRating, star: number): boolean {
    const effective = userRating.hoverRating > 0 ? userRating.hoverRating : userRating.rating;
    return effective >= star;
  }

  starAriaLabel(star: number): string {
    return `${star - 0.5} to ${star} stars`;
  }

  ratingLabel(userRating: UserRating): string {
    return userRating.rating > 0 ? `${userRating.rating} out of 5` : 'No rating';
  }

  // ---------------------------------------------------------------------------
  // Inline review — submit (AC9)
  // ---------------------------------------------------------------------------

  onSubmitReview(): void {
    this.reviewRatingError = null;
    this.reviewSaveError = null;

    const validRatings = new Set(Array.from({ length: 11 }, (_, i) => i * 0.5));
    const reviews: CookReviewPayload[] = [];

    for (const ur of this.userRatings) {
      if (ur.rating > 0) {
        if (!validRatings.has(ur.rating)) {
          this.reviewRatingError = 'Rating must be between 0 and 5 in 0.5 steps.';
          return;
        }
        reviews.push({ userId: ur.userId, rating: ur.rating, notes: ur.notes.trim() || null });
      }
    }

    if (reviews.length === 0) {
      this.reviewRatingError = 'Please add at least one rating before submitting.';
      return;
    }

    const payload: CompleteCookPayload = {
      portions: this.cookInstance?.portions ?? null,
      reviews
    };

    this.isSavingReview = true;

    this.cookApi.completeCook(this.cookInstanceId, payload).subscribe({
      next: (updated) => {
        this.isSavingReview = false;
        // Update the cook instance with the new reviews so the ratings row appears
        this.cookInstance = updated;
        this.reviewSubmitted = true;
      },
      error: () => {
        this.isSavingReview = false;
        this.reviewSaveError = 'Could not save review. Please try again.';
      }
    });
  }
}
