import {
  Component,
  EventEmitter,
  HostListener,
  OnDestroy,
  OnInit,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { GeoffFilterApiService } from '../../../core/services/geoff-filter-api.service';
import { UserStateService } from '../../../core/services/user-state.service';

/**
 * Modal dialog for submitting a recipe suggestion.
 * Self-contained: reads active user from UserStateService, POSTs to /api/recipe-suggestions.
 * Emits (dismissed) on cancel, close, backdrop click, or successful submission.
 */
@Component({
  selector: 'app-submit-suggestion-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './submit-suggestion-dialog.component.html',
  styleUrl: './submit-suggestion-dialog.component.scss'
})
export class SubmitSuggestionDialogComponent implements OnInit, OnDestroy {

  /** Emitted when the dialog should be removed from the DOM. */
  @Output() dismissed = new EventEmitter<void>();

  // Active user state
  activeUserId = 0;
  activeUserName = '';

  // Form fields
  urlValue = '';
  textValue = '';

  // Submission state
  isSubmitting = false;
  submitError: string | null = null;

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly api: GeoffFilterApiService,
    private readonly userState: UserStateService,
    private readonly snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    // Snapshot initial values
    this.activeUserId = this.userState.activeUserId;
    this.activeUserName = this.userState.activeUserName;

    // Subscribe live — spec requires the indicator to update if user toggles while modal is open
    this.userState.activeUserId$
      .pipe(takeUntil(this.destroy$))
      .subscribe(id => { this.activeUserId = id; });

    this.userState.activeUserName$
      .pipe(takeUntil(this.destroy$))
      .subscribe(name => { this.activeUserName = name; });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ---------------------------------------------------------------------------
  // Derived state
  // ---------------------------------------------------------------------------

  get hasContent(): boolean {
    return this.urlValue.trim().length > 0 || this.textValue.trim().length > 0;
  }

  get canSubmit(): boolean {
    return this.hasContent && !this.isSubmitting;
  }

  // ---------------------------------------------------------------------------
  // Actions
  // ---------------------------------------------------------------------------

  onSubmit(): void {
    if (!this.canSubmit) return;

    this.isSubmitting = true;
    this.submitError = null;

    this.api.createSuggestion({
      suggestedBy: this.activeUserId,
      suggestionUrl: this.urlValue.trim() || null,
      suggestionText: this.textValue.trim() || null
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.snackBar.open('Suggestion submitted!', undefined, { duration: 3000 });
        this.dismissed.emit();
      },
      error: () => {
        this.isSubmitting = false;
        this.submitError = 'Failed to submit suggestion. Please try again.';
      }
    });
  }

  onCancel(): void {
    if (!this.isSubmitting) {
      this.dismissed.emit();
    }
  }

  onBackdropClick(): void {
    if (!this.isSubmitting) {
      this.dismissed.emit();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.isSubmitting) {
      this.dismissed.emit();
    }
  }
}
