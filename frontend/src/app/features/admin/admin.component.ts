import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject, takeUntil } from 'rxjs';

import { MatExpansionModule } from '@angular/material/expansion';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';

import { RecipeApiService } from '../../core/services/recipe-api.service';
import { UnitsService } from '../../core/services/units.service';
import { IngredientOptionDto, UnitOptionDto, AdminReviewDto } from '../../core/models/recipe.models';
import { IngredientCasePipe } from '../../shared/pipes/ingredient-case.pipe';

interface IngredientRow {
  ingredient: IngredientOptionDto;
  /** Inline error message shown beneath the delete button, or null when idle. */
  error: string | null;
  /** True while a delete request is in flight for this row. */
  deleting: boolean;
}

interface ReviewRow {
  review: AdminReviewDto;
  error: string | null;
  deleting: boolean;
}

type LoadState = 'loading' | 'ready' | 'error';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatExpansionModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatIconModule,
    IngredientCasePipe,
  ],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit, OnDestroy {
  private readonly api = inject(RecipeApiService);
  readonly unitsService = inject(UnitsService);
  private readonly fb = inject(FormBuilder);
  private readonly destroy$ = new Subject<void>();

  // -------------------------------------------------------------------------
  // Ingredients section
  // -------------------------------------------------------------------------
  ingredientsState: LoadState = 'loading';
  ingredientRows: IngredientRow[] = [];
  ingredientSearch = '';

  get filteredIngredientRows(): IngredientRow[] {
    const term = this.ingredientSearch.toLowerCase().trim();
    if (!term) return this.ingredientRows;
    return this.ingredientRows.filter(r =>
      r.ingredient.name.toLowerCase().includes(term)
    );
  }

  // -------------------------------------------------------------------------
  // Units section
  // -------------------------------------------------------------------------
  units: UnitOptionDto[] = [];
  unitSearch = '';

  get filteredUnits(): UnitOptionDto[] {
    const term = this.unitSearch.toLowerCase().trim();
    if (!term) return this.units;
    return this.units.filter(u =>
      u.name.toLowerCase().includes(term) ||
      (u.abbreviation?.toLowerCase().includes(term) ?? false)
    );
  }

  readonly unitForm = this.fb.group({
    name: ['', Validators.required],
    abbreviation: [''],
  });
  unitFormError: string | null = null;
  unitFormSaving = false;

  // -------------------------------------------------------------------------
  // Reviews section
  // -------------------------------------------------------------------------
  reviewsState: LoadState = 'loading';
  reviewRows: ReviewRow[] = [];
  reviewSearch = '';

  get filteredReviewRows(): ReviewRow[] {
    const term = this.reviewSearch.toLowerCase().trim();
    if (!term) return this.reviewRows;
    return this.reviewRows.filter(r =>
      r.review.recipeTitle.toLowerCase().includes(term) ||
      r.review.userName.toLowerCase().includes(term)
    );
  }

  // -------------------------------------------------------------------------
  // Lifecycle
  // -------------------------------------------------------------------------

  ngOnInit(): void {
    this.loadIngredients();
    this.loadReviews();

    // Ensure units are loaded; UnitsService is a singleton so this is safe to
    // call even if the form page has already triggered it.
    this.unitsService.refresh();
    this.unitsService.units$
      .pipe(takeUntil(this.destroy$))
      .subscribe(units => {
        this.units = units;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // -------------------------------------------------------------------------
  // Ingredients
  // -------------------------------------------------------------------------

  private loadIngredients(): void {
    this.ingredientsState = 'loading';
    this.api.getAllIngredients().subscribe({
      next: (list) => {
        this.ingredientRows = list.map(i => ({ ingredient: i, error: null, deleting: false }));
        this.ingredientsState = 'ready';
      },
      error: () => {
        this.ingredientsState = 'error';
      },
    });
  }

  deleteIngredient(row: IngredientRow): void {
    row.error = null;
    row.deleting = true;

    this.api.deleteIngredient(row.ingredient.id).subscribe({
      next: () => {
        this.ingredientRows = this.ingredientRows.filter(r => r !== row);
      },
      error: (err: HttpErrorResponse) => {
        row.deleting = false;
        if (err.status === 409 && err.error?.message) {
          row.error = err.error.message;
        } else {
          row.error = 'An error occurred — please try again.';
        }
      },
    });
  }

  // -------------------------------------------------------------------------
  // Units — add unit form
  // -------------------------------------------------------------------------

  submitUnitForm(): void {
    if (this.unitForm.invalid) return;

    this.unitFormError = null;
    this.unitFormSaving = true;

    const name = this.unitForm.value.name!.trim();
    const abbreviation = this.unitForm.value.abbreviation?.trim() || undefined;

    this.unitsService.createUnit(name, abbreviation).subscribe({
      next: () => {
        this.unitFormSaving = false;
        this.unitForm.reset();
      },
      error: (err: HttpErrorResponse) => {
        this.unitFormSaving = false;
        if (err.status === 409) {
          this.unitFormError = 'A unit with this name already exists.';
        } else {
          this.unitFormError = 'An error occurred — please try again.';
        }
      },
    });
  }

  // -------------------------------------------------------------------------
  // Reviews
  // -------------------------------------------------------------------------

  private loadReviews(): void {
    this.reviewsState = 'loading';
    this.api.getAllReviews().subscribe({
      next: (list) => {
        this.reviewRows = list.map(r => ({ review: r, error: null, deleting: false }));
        this.reviewsState = 'ready';
      },
      error: () => {
        this.reviewsState = 'error';
      },
    });
  }

  deleteReview(row: ReviewRow): void {
    row.error = null;
    row.deleting = true;

    this.api.deleteReview(row.review.id).subscribe({
      next: () => {
        this.reviewRows = this.reviewRows.filter(r => r !== row);
      },
      error: () => {
        row.deleting = false;
        row.error = 'An error occurred — please try again.';
      },
    });
  }

  /** Format a rating as filled/empty stars for display. */
  ratingLabel(rating: number): string {
    return `★ ${rating}`;
  }
}
