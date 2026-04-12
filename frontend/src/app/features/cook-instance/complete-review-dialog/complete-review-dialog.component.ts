import {
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnInit,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CompleteCookPayload, CookReviewPayload } from '../../../core/models/cook-instance.models';

/** One user's rating state in the modal. */
interface UserRating {
  userId: number;
  userName: string;
  rating: number;       // 0 = unset, 0.5–5.0 in 0.5 increments
  hoverRating: number;  // transient hover preview (0 = no hover)
  notes: string;
}

@Component({
  selector: 'app-complete-review-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './complete-review-dialog.component.html',
  styleUrl: './complete-review-dialog.component.scss'
})
export class CompleteReviewDialogComponent implements OnInit {
  /** The cook instance id being completed. */
  @Input({ required: true }) cookInstanceId!: number;
  /** Initial portions value from the cook instance. */
  @Input() initialPortions: number | null = null;
  /** Emitted when the modal closes (with payload) or is dismissed (skip). */
  @Output() completed = new EventEmitter<CompleteCookPayload>();
  /** Emitted when the overlay backdrop is clicked (close without completing). */
  @Output() dismissed = new EventEmitter<void>();

  // Form state
  portions: number | null = null;

  // Error state
  ratingError: string | null = null;
  saveError: string | null = null;
  isSaving = false;

  /** Geoff (id=1) and Helen (id=2) rating rows. */
  userRatings: UserRating[] = [];

  /** Star values 1–5 for template iteration. */
  readonly starValues = [1, 2, 3, 4, 5];

  ngOnInit(): void {
    this.portions = this.initialPortions;
    this.userRatings = [
      { userId: 1, userName: 'Geoff', rating: 0, hoverRating: 0, notes: '' },
      { userId: 2, userName: 'Helen', rating: 0, hoverRating: 0, notes: '' }
    ];
  }

  // ---------------------------------------------------------------------------
  // Half-star interaction
  // ---------------------------------------------------------------------------

  /**
   * Determines the rating value for a click at a given x position within a star element.
   * Left half → star - 0.5, right half → star.
   */
  private halfStarValue(star: number, event: MouseEvent): number {
    const target = event.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const x = event.clientX - rect.left;
    return x < rect.width / 2 ? star - 0.5 : star;
  }

  onStarClick(userRating: UserRating, star: number, event: MouseEvent): void {
    userRating.rating = this.halfStarValue(star, event);
    userRating.hoverRating = 0;
    this.ratingError = null;
  }

  onStarHover(userRating: UserRating, star: number, event: MouseEvent): void {
    userRating.hoverRating = this.halfStarValue(star, event);
  }

  onStarLeave(userRating: UserRating): void {
    userRating.hoverRating = 0;
  }

  /**
   * Returns whether the left half of a star should be filled.
   * Filled if effective rating >= star - 0.5.
   */
  leftHalfFilled(userRating: UserRating, star: number): boolean {
    const effective = userRating.hoverRating > 0 ? userRating.hoverRating : userRating.rating;
    return effective >= star - 0.5;
  }

  /**
   * Returns whether the right half of a star should be filled.
   * Filled if effective rating >= star (i.e. full star).
   */
  rightHalfFilled(userRating: UserRating, star: number): boolean {
    const effective = userRating.hoverRating > 0 ? userRating.hoverRating : userRating.rating;
    return effective >= star;
  }

  /** Aria label for a star button. */
  starAriaLabel(star: number): string {
    return `${star - 0.5} to ${star} stars`;
  }

  /** Displayed rating text for screen readers. */
  ratingLabel(userRating: UserRating): string {
    return userRating.rating > 0 ? `${userRating.rating} out of 5` : 'No rating';
  }

  // ---------------------------------------------------------------------------
  // Actions
  // ---------------------------------------------------------------------------

  onSaveReview(): void {
    this.ratingError = null;
    this.saveError = null;

    // Validate any ratings that have been set are in the valid set
    const validRatings = new Set(
      Array.from({ length: 11 }, (_, i) => i * 0.5)
    );

    const reviews: CookReviewPayload[] = [];
    for (const ur of this.userRatings) {
      if (ur.rating > 0) {
        if (!validRatings.has(ur.rating)) {
          this.ratingError = 'Rating must be between 0 and 5 in 0.5 steps.';
          return;
        }
        reviews.push({ userId: ur.userId, rating: ur.rating, notes: ur.notes.trim() || null });
      }
    }

    const payload: CompleteCookPayload = {
      portions: this.portions,
      reviews
    };

    this.completed.emit(payload);
  }

  onSkip(): void {
    const payload: CompleteCookPayload = {
      portions: this.portions,
      reviews: []
    };
    this.completed.emit(payload);
  }

  onBackdropClick(): void {
    this.dismissed.emit();
  }

  /** Close dialog when Escape is pressed. */
  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.dismissed.emit();
  }
}
