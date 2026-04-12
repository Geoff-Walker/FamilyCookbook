import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { CookInstanceApiService } from '../../../core/services/cook-instance-api.service';
import { CompleteCookPayload, CookInstanceDetailDto } from '../../../core/models/cook-instance.models';
import {
  IngredientChecklistComponent,
  IngredientPatchEvent
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
    this.cookApi.patchIngredient(this.cookInstanceId, event.ingredientId, event.patch).subscribe({
      error: () => {
        // Non-fatal — the UI state is already updated optimistically.
        // A future ticket can add error recovery.
      }
    });
  }

  /** "Complete Cook" button clicked — open the review modal. */
  onCompleteCook(): void {
    this.showReviewDialog = true;
  }

  /** The review dialog emitted a payload (Save Review or Do this later). */
  onReviewCompleted(payload: CompleteCookPayload): void {
    this.showReviewDialog = false;
    this.cookApi.completeCook(this.cookInstanceId, payload).subscribe({
      next: (updated) => {
        this.cookInstance = updated;
        // Determine new status from whether reviews were submitted
        if (payload.reviews.length > 0) {
          this.cookStatus = 'completed';
        } else {
          this.cookStatus = 'awaitingReview';
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
