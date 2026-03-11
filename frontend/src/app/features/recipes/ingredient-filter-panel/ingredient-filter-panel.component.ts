import {
  Component,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  Subject,
  debounceTime,
  distinctUntilChanged,
  switchMap,
  takeUntil,
  of
} from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { IngredientOptionDto } from '../../../core/models/recipe.models';

export interface IngredientFilterState {
  /** True when at least one ingredient chip is selected */
  hasFilters: boolean;
  /** Currently selected ingredient IDs (non-empty when hasFilters is true) */
  ingredientIds: number[];
}

export interface SelectedIngredient {
  id: number;
  name: string;
  /** Animation state — 'in' = visible, 'out' = being removed */
  animState: 'in' | 'out';
}

@Component({
  selector: 'app-ingredient-filter-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ingredient-filter-panel.component.html',
  styleUrl: './ingredient-filter-panel.component.scss'
})
export class IngredientFilterPanelComponent implements OnInit, OnDestroy {
  /** Emits whenever the selected ingredient set changes (add, remove, clear). */
  @Output() filterStateChange = new EventEmitter<IngredientFilterState>();

  // -------------------------------------------------------------------------
  // Panel open/close
  // -------------------------------------------------------------------------

  isOpen = false;

  // -------------------------------------------------------------------------
  // Chip (selected ingredients) state
  // -------------------------------------------------------------------------

  selectedIngredients: SelectedIngredient[] = [];

  // -------------------------------------------------------------------------
  // Autocomplete input state
  // -------------------------------------------------------------------------

  inputValue = '';
  suggestions: IngredientOptionDto[] = [];
  isSuggestionsOpen = false;
  isLoadingSuggestions = false;

  /** Shake animation flag — toggled for duplicate-entry feedback (AC-D9) */
  isShaking = false;

  // -------------------------------------------------------------------------
  // Private
  // -------------------------------------------------------------------------

  private readonly inputSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(private readonly recipeApi: RecipeApiService) {}

  ngOnInit(): void {
    // Debounced autocomplete — AC2 (400 ms), AC9 (skip empty)
    this.inputSubject.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      takeUntil(this.destroy$),
      switchMap(term => {
        if (term.length < 2) {
          // AC9: fewer than 2 chars → no dropdown, no API call
          this.suggestions = [];
          this.isSuggestionsOpen = false;
          this.isLoadingSuggestions = false;
          return of([]);
        }

        this.isLoadingSuggestions = true;
        return this.recipeApi.searchIngredients(term).pipe(
          catchError(() => {
            this.isLoadingSuggestions = false;
            return of([] as IngredientOptionDto[]);
          })
        );
      })
    ).subscribe(results => {
      this.isLoadingSuggestions = false;
      // Filter out already-selected ingredients
      this.suggestions = results.filter(
        r => !this.selectedIngredients.some(s => s.id === r.id)
      );
      this.isSuggestionsOpen = this.suggestions.length > 0;
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // -------------------------------------------------------------------------
  // Panel toggle
  // -------------------------------------------------------------------------

  togglePanel(): void {
    this.isOpen = !this.isOpen;
    if (!this.isOpen) {
      this.closeSuggestions();
    }
  }

  // -------------------------------------------------------------------------
  // Input handling
  // -------------------------------------------------------------------------

  onInputChange(value: string): void {
    this.inputValue = value;
    this.inputSubject.next(value);
  }

  onInputBlur(): void {
    // Delay close so a click on a suggestion registers first
    setTimeout(() => this.closeSuggestions(), 150);
  }

  // -------------------------------------------------------------------------
  // Selecting an ingredient from the dropdown (AC3)
  // -------------------------------------------------------------------------

  selectIngredient(ingredient: IngredientOptionDto): void {
    if (this.selectedIngredients.some(s => s.id === ingredient.id)) {
      // Already selected — shake and bail (AC-D9)
      this.triggerShake();
      this.closeSuggestions();
      return;
    }

    this.selectedIngredients = [
      ...this.selectedIngredients,
      { id: ingredient.id, name: ingredient.name, animState: 'in' }
    ];
    this.inputValue = '';
    this.closeSuggestions();
    this.emitState();
  }

  // -------------------------------------------------------------------------
  // Removing an ingredient chip (AC4, AC-D7)
  // -------------------------------------------------------------------------

  removeIngredient(id: number): void {
    // Mark the chip as 'out' to trigger the scale-down animation
    this.selectedIngredients = this.selectedIngredients.map(s =>
      s.id === id ? { ...s, animState: 'out' as const } : s
    );

    // Remove from DOM after the 150ms animation
    setTimeout(() => {
      this.selectedIngredients = this.selectedIngredients.filter(s => s.id !== id);
      this.emitState();
    }, 150);
  }

  // -------------------------------------------------------------------------
  // Clear all (AC8, AC-D8)
  // -------------------------------------------------------------------------

  clearAll(): void {
    this.selectedIngredients = [];
    this.inputValue = '';
    this.closeSuggestions();
    this.emitState();
  }

  // -------------------------------------------------------------------------
  // Duplicate shake (AC-D9)
  // -------------------------------------------------------------------------

  private triggerShake(): void {
    this.isShaking = true;
    setTimeout(() => { this.isShaking = false; }, 300);
  }

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  get selectedCount(): number {
    return this.selectedIngredients.filter(s => s.animState === 'in').length;
  }

  private closeSuggestions(): void {
    this.suggestions = [];
    this.isSuggestionsOpen = false;
  }

  private emitState(): void {
    const activeIds = this.selectedIngredients
      .filter(s => s.animState === 'in')
      .map(s => s.id);

    this.filterStateChange.emit({
      hasFilters: activeIds.length > 0,
      ingredientIds: activeIds
    });
  }
}
