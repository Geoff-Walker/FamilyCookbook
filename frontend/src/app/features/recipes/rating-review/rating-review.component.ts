import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { UserStateService } from '../../../core/services/user-state.service';
import { ReviewDto } from '../../../core/models/recipe.models';
import { UserDto } from '../../../core/models/recipe.models';

type SaveState = 'idle' | 'saving' | 'success' | 'error';

interface UserCardState {
  user: UserDto;
  /** Most recent review for this user (null = never reviewed) */
  latestReview: ReviewDto | null;
  /** Pending star selection (1–5) for the interactive card */
  pendingRating: number;
  /** Pending notes text for the interactive card */
  pendingNotes: string;
  /** Hover preview star (0 = no hover) — only used on the active card */
  hoverRating: number;
  saveState: SaveState;
  errorMessage: string | null;
}

@Component({
  selector: 'app-rating-review',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './rating-review.component.html',
  styleUrl: './rating-review.component.scss'
})
export class RatingReviewComponent implements OnInit, OnDestroy {
  @Input({ required: true }) recipeId!: number;

  cards: UserCardState[] = [];
  isLoading = true;
  loadError = false;

  private activeUserId = 0;
  private readonly destroy$ = new Subject<void>();

  readonly starValues = [1, 2, 3, 4, 5];

  constructor(
    private readonly recipeApi: RecipeApiService,
    private readonly userState: UserStateService,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.activeUserId = this.userState.activeUserId;

    // React to active user changes
    this.userState.activeUserId$.pipe(takeUntil(this.destroy$)).subscribe(id => {
      this.activeUserId = id;
      // Re-apply pending state from the new active user's existing review
      this.refreshPendingFromReviews();
    });

    // Seed cards from the user list, then load reviews
    const users = this.userState.users;
    this.cards = users.map(u => this.buildEmptyCard(u));
    this.loadReviews();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ---------------------------------------------------------------------------
  // Data loading
  // ---------------------------------------------------------------------------

  private loadReviews(): void {
    this.isLoading = true;
    this.loadError = false;

    this.recipeApi.getReviews(this.recipeId).subscribe({
      next: (reviews) => {
        this.applyReviews(reviews);
        this.isLoading = false;
      },
      error: () => {
        this.loadError = true;
        this.isLoading = false;
      }
    });
  }

  retryLoad(): void {
    this.loadReviews();
  }

  private applyReviews(reviews: ReviewDto[]): void {
    for (const card of this.cards) {
      // Most recent review for this user
      const userReviews = reviews
        .filter(r => r.userId === card.user.id)
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

      card.latestReview = userReviews[0] ?? null;

      // Pre-populate the pending fields if this is the active user (AC3)
      if (card.user.id === this.activeUserId && card.latestReview) {
        card.pendingRating = card.latestReview.rating;
        card.pendingNotes = card.latestReview.notes ?? '';
      }
    }
  }

  private refreshPendingFromReviews(): void {
    for (const card of this.cards) {
      if (card.user.id === this.activeUserId && card.latestReview) {
        card.pendingRating = card.latestReview.rating;
        card.pendingNotes = card.latestReview.notes ?? '';
      }
    }
  }

  private buildEmptyCard(user: UserDto): UserCardState {
    return {
      user,
      latestReview: null,
      pendingRating: 0,
      pendingNotes: '',
      hoverRating: 0,
      saveState: 'idle',
      errorMessage: null
    };
  }

  // ---------------------------------------------------------------------------
  // Star interaction helpers
  // ---------------------------------------------------------------------------

  isActiveUser(card: UserCardState): boolean {
    return card.user.id === this.activeUserId;
  }

  /** Displayed star fill for a given star index on the active (interactive) card. */
  starFilled(card: UserCardState, star: number): boolean {
    const effective = card.hoverRating > 0 ? card.hoverRating : card.pendingRating;
    return star <= effective;
  }

  /** Read-only star fill for the other user's card. */
  readonlyStarFilled(card: UserCardState, star: number): boolean {
    if (!card.latestReview) return false;
    return star <= card.latestReview.rating;
  }

  onStarClick(card: UserCardState, star: number): void {
    if (!this.isActiveUser(card) || card.saveState === 'saving') return;
    card.pendingRating = star;
  }

  onStarHover(card: UserCardState, star: number): void {
    if (!this.isActiveUser(card) || card.saveState === 'saving') return;
    card.hoverRating = star;
  }

  onStarLeave(card: UserCardState): void {
    card.hoverRating = 0;
  }

  // ---------------------------------------------------------------------------
  // Save
  // ---------------------------------------------------------------------------

  canSave(card: UserCardState): boolean {
    return this.isActiveUser(card)
      && card.pendingRating > 0
      && card.saveState !== 'saving';
  }

  saveRating(card: UserCardState): void {
    if (!this.canSave(card)) return;

    card.saveState = 'saving';
    card.errorMessage = null;

    const today = new Date().toISOString().split('T')[0]; // AC9: ISO date string

    this.recipeApi.createReview(this.recipeId, {
      userId: card.user.id,
      rating: card.pendingRating,
      notes: card.pendingNotes.trim() || null,
      madeOn: today
    }).subscribe({
      next: (review) => {
        card.latestReview = review;
        card.saveState = 'idle';
        this.snackBar.open('Rating saved', undefined, { duration: 2000 });
      },
      error: () => {
        card.saveState = 'error';
        card.errorMessage = 'Could not save rating. Please try again.';
      }
    });
  }

  // ---------------------------------------------------------------------------
  // Display helpers
  // ---------------------------------------------------------------------------

  /** Last-cooked date formatted as "12 Mar 2026" (AC-D10). */
  formatLastCooked(review: ReviewDto | null): string {
    if (!review?.madeOn) return 'Not yet cooked';
    return new Date(review.madeOn).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }
}
