import { Component, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { CookInstanceApiService } from '../../../core/services/cook-instance-api.service';
import { CompleteCookPayload, CookInstanceDetailDto } from '../../../core/models/cook-instance.models';
import {
  IngredientChecklistComponent,
  IngredientPatchEvent,
  IngredientRemoveEvent
} from '../ingredient-checklist/ingredient-checklist.component';
import { CompleteReviewDialogComponent } from '../complete-review-dialog/complete-review-dialog.component';
import { HeaderStateService } from '../../../core/services/header-state.service';

type ViewState = 'loading' | 'loaded' | 'notFound' | 'error';

/** Status of the cook instance from the page's perspective. */
type CookStatus = 'inProgress' | 'awaitingReview' | 'completed';

@Component({
  selector: 'app-cook-instance-page',
  standalone: true,
  imports: [CommonModule, RouterLink, IngredientChecklistComponent, CompleteReviewDialogComponent],
  templateUrl: './cook-instance-page.component.html',
  styleUrl: './cook-instance-page.component.scss'
})
export class CookInstancePageComponent implements OnInit {
  viewState: ViewState = 'loading';
  cookInstance: CookInstanceDetailDto | null = null;
  cookInstanceId!: number;

  /** Whether the review modal is open. */
  showReviewDialog = false;

  /** Captured from the checklist's scaledPortions when Complete Cook is tapped. */
  effectivePortions: number | null = null;

  @ViewChild(IngredientChecklistComponent)
  private checklist?: IngredientChecklistComponent;

  /** Derived cook status for the status pill. */
  cookStatus: CookStatus = 'inProgress';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly cookApi: CookInstanceApiService,
    private readonly headerState: HeaderStateService
  ) {}

  ngOnInit(): void {
    this.cookInstanceId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  load(): void {
    this.viewState = 'loading';
    this.cookApi.getCookInstance(this.cookInstanceId).subscribe({
      next: (data) => {
        // Guard: if the cook is already completed, redirect to the view page (WAL-76)
        if (data.completedAt) {
          this.router.navigate(['/cook', this.cookInstanceId], { replaceUrl: true });
          return;
        }
        this.cookInstance = data;
        this.viewState = 'loaded';
        this.headerState.setPageTitle(data.recipeTitle);
        this.deriveCookStatus(data);
      },
      error: (err: HttpErrorResponse) => {
        this.viewState = err.status === 404 ? 'notFound' : 'error';
        this.headerState.setPageTitle(null);
      }
    });
  }

  /**
   * Derive the pill status from the cook instance data.
   * - completedAt null → In Progress
   * - completedAt set, no reviews yet → Awaiting Review
   * - completedAt set, reviews present → Completed
   *
   * Note: the backend does not return reviews in the detail DTO currently;
   * once WAL-74 adds that, this logic will be extended. For now the modal
   * sets status locally after a successful complete call.
   */
  private deriveCookStatus(data: CookInstanceDetailDto): void {
    if (!data.completedAt) {
      this.cookStatus = 'inProgress';
    } else if (data.reviews && data.reviews.length > 0) {
      this.cookStatus = 'completed';
    } else {
      this.cookStatus = 'awaitingReview';
    }
  }

  onIngredientPatched(event: IngredientPatchEvent): void {
    if (!this.cookInstance) return;
    console.log('[CookPage] ingredientPatched — sending PATCH', {
      cookInstanceId: this.cookInstanceId,
      ingredientId: event.ingredientId,
      patch: event.patch
    });
    this.cookApi.patchIngredient(this.cookInstanceId, event.ingredientId, event.patch).subscribe({
      next: () => {
        console.log('[CookPage] PATCH succeeded for ingredient', event.ingredientId);
      },
      error: (err) => {
        console.error('[CookPage] PATCH failed for ingredient', event.ingredientId, err);
        // Non-fatal — the UI state is already updated optimistically.
      }
    });
  }

  onIngredientRemoved(event: IngredientRemoveEvent): void {
    if (!this.cookInstance) return;
    this.cookApi.removeCookIngredient(this.cookInstanceId, event.ingredientId).subscribe({
      next: () => {
        // Remove the ingredient from the local stageGroups so the checklist re-renders without it
        if (!this.cookInstance) return;
        this.cookInstance = {
          ...this.cookInstance,
          stageGroups: this.cookInstance.stageGroups
            .map(stage => ({
              ...stage,
              ingredients: stage.ingredients.filter(i => i.id !== event.ingredientId)
            }))
            .filter(stage => stage.ingredients.length > 0)
        };
      },
      error: () => {
        // API failed — reload to restore consistent state
        this.load();
      }
    });
  }

  /** Whether the inline cancel confirmation is visible. */
  showCancelConfirm = false;

  /** "Complete Cook" button clicked — open the review modal. */
  onCompleteCook(): void {
    // Flush any amount change that is still held in a focused input field.
    // The PATCH fires on blur; if the user taps Complete without leaving the input,
    // the blur event is triggered here so the last change is not lost.
    (document.activeElement as HTMLElement)?.blur();
    // Capture the checklist's current scaled portions (accounts for limiter scaling).
    // Falls back to the cook instance's stored portions if no checklist is mounted.
    this.effectivePortions = this.checklist?.scaledPortions ?? this.cookInstance?.portions ?? null;
    this.showReviewDialog = true;
  }

  /** "Cancel Cook" button clicked — show inline confirmation. */
  onCancelCook(): void {
    this.showCancelConfirm = true;
  }

  /** Confirmed cancel — soft-delete this cook instance and navigate back to the recipe. */
  onCancelConfirmed(): void {
    if (!this.cookInstance) return;
    const recipeId = this.cookInstance.recipeId;
    this.cookApi.deleteCookInstance(this.cookInstanceId).subscribe({
      next: () => {
        this.router.navigate(['/recipes', recipeId]);
      },
      error: () => {
        // Reset confirmation state so the user can try again or navigate away manually
        this.showCancelConfirm = false;
      }
    });
  }

  /** Back button on cancel confirmation — dismiss without cancelling. */
  onCancelDismissed(): void {
    this.showCancelConfirm = false;
  }

  /** The review dialog emitted a payload (Save Review or Do this later). */
  onReviewCompleted(payload: CompleteCookPayload): void {
    this.showReviewDialog = false;
    this.cookApi.completeCook(this.cookInstanceId, payload).subscribe({
      next: (updated) => {
        this.cookInstance = updated;
        if (payload.reviews.length > 0) {
          // Reviewed — navigate to recipe detail
          this.router.navigate(['/recipes', updated.recipeId]);
        } else {
          // Cook is complete but no review yet — go to the view page (WAL-76)
          this.router.navigate(['/cook', this.cookInstanceId], { replaceUrl: true });
        }
      },
      error: () => {
        // Surface error gracefully — keep modal closed, page remains usable
      }
    });
  }

  /** The dialog backdrop was clicked — dismiss without completing. */
  onDialogDismissed(): void {
    this.showReviewDialog = false;
  }

  // ---------------------------------------------------------------------------
  // Pill helpers
  // ---------------------------------------------------------------------------

  get pillLabel(): string {
    switch (this.cookStatus) {
      case 'inProgress':    return 'In Progress';
      case 'awaitingReview': return 'Awaiting Review';
      case 'completed':     return 'Completed';
    }
  }

  get pillClass(): string {
    switch (this.cookStatus) {
      case 'inProgress':    return 'status-pill--in-progress';
      case 'awaitingReview': return 'status-pill--awaiting';
      case 'completed':     return 'status-pill--completed';
    }
  }

  formatStartedAt(iso: string): string {
    return new Date(iso).toLocaleTimeString('en-GB', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    });
  }

  starDisplay(rating: number): string {
    const filled = Math.floor(rating);
    const half = rating % 1 >= 0.5;
    return '★'.repeat(filled) + (half ? '½' : '') + '☆'.repeat(5 - filled - (half ? 1 : 0));
  }
}
