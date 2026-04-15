import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  OnInit,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, takeUntil, catchError } from 'rxjs';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { RecipeSummaryDto } from '../../../core/models/recipe.models';
import { MealPlanSlotDto } from '../../../core/models/meal-planner.models';
import { MealPlannerApiService } from '../../../core/services/meal-planner-api.service';
import { BatchStepperComponent } from '../batch-stepper/batch-stepper.component';

/**
 * Modal dialog for adding a recipe slot to a specific date.
 * The date is passed in via @Input — no date field is shown in the modal.
 * Emits (slotAdded) on successful POST, (dismissed) on cancel/close.
 */
@Component({
  selector: 'app-add-to-planner-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, BatchStepperComponent],
  templateUrl: './add-to-planner-dialog.component.html',
  styleUrl: './add-to-planner-dialog.component.scss'
})
export class AddToPlannerDialogComponent implements OnInit, OnDestroy {
  /** ISO date string for the target day cell e.g. "2026-04-15". */
  @Input({ required: true }) slotDate!: string;

  /** Emitted with the newly created slot on successful POST. */
  @Output() slotAdded = new EventEmitter<MealPlanSlotDto>();

  /** Emitted when the dialog is cancelled or dismissed. */
  @Output() dismissed = new EventEmitter<void>();

  // Search state
  searchQuery = '';
  searchResults: RecipeSummaryDto[] = [];
  selectedRecipe: RecipeSummaryDto | null = null;
  isSearching = false;
  showDropdown = false;

  // Batch multiplier
  batchMultiplier = 1;

  // Submission state
  isSaving = false;
  saveError: string | null = null;

  private readonly searchInput$ = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly recipeApi: RecipeApiService,
    private readonly mealPlannerApi: MealPlannerApiService
  ) {}

  ngOnInit(): void {
    // Debounced recipe search: 300ms, only trigger on distinct changes
    this.searchInput$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        const trimmed = query.trim();
        if (trimmed.length < 1) {
          this.searchResults = [];
          this.showDropdown = false;
          this.isSearching = false;
          return of([]);
        }
        this.isSearching = true;
        // Use plain recipe list search — filters by title match
        return this.recipeApi.getRecipes().pipe(
          catchError(() => of([] as RecipeSummaryDto[]))
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe(results => {
      this.isSearching = false;
      if (this.searchQuery.trim().length > 0 && !this.selectedRecipe) {
        const q = this.searchQuery.trim().toLowerCase();
        this.searchResults = (results as RecipeSummaryDto[])
          .filter(r => r.title.toLowerCase().includes(q))
          .slice(0, 20);
        this.showDropdown = this.searchResults.length > 0;
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get canSubmit(): boolean {
    return this.selectedRecipe !== null && !this.isSaving;
  }

  onSearchInput(): void {
    if (this.selectedRecipe) return; // already selected
    this.searchInput$.next(this.searchQuery);
  }

  selectRecipe(recipe: RecipeSummaryDto): void {
    this.selectedRecipe = recipe;
    this.searchQuery = recipe.title;
    this.showDropdown = false;
    this.searchResults = [];
  }

  clearRecipe(): void {
    this.selectedRecipe = null;
    this.searchQuery = '';
    this.searchResults = [];
    this.showDropdown = false;
  }

  onBatchChange(value: number): void {
    this.batchMultiplier = value;
  }

  onSubmit(): void {
    if (!this.canSubmit || !this.selectedRecipe) return;
    this.isSaving = true;
    this.saveError = null;

    this.mealPlannerApi.createSlot({
      slotDate: this.slotDate,
      slotType: 'recipe',
      recipeId: this.selectedRecipe.id,
      batchMultiplier: Math.floor(this.batchMultiplier),
      notes: null,
      sortOrder: 0
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (slot) => {
        this.isSaving = false;
        this.slotAdded.emit(slot);
      },
      error: () => {
        this.isSaving = false;
        this.saveError = 'Failed to add to planner. Please try again.';
      }
    });
  }

  onCancel(): void {
    this.dismissed.emit();
  }

  onBackdropClick(): void {
    if (!this.isSaving) {
      this.dismissed.emit();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.isSaving) {
      this.dismissed.emit();
    }
  }
}
