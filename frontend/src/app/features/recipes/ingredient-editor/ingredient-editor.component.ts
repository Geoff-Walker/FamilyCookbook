import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
  inject
} from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { IngredientOptionDto, UnitOptionDto } from '../../../core/models/recipe.models';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { UnitsService } from '../../../core/services/units.service';
import { IngredientCasePipe } from '../../../shared/pipes/ingredient-case.pipe';

/** Sentinel string used to tag the "Add as new ingredient" option. */
const ADD_NEW_SENTINEL = '__ADD_NEW__';

export interface SuggestionItem {
  id: number | string;
  name: string;
  isAddNew: boolean;
}

@Component({
  selector: 'app-ingredient-editor',
  standalone: true,
  imports: [
    AsyncPipe,
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatFormFieldModule,
    MatInputModule,
    IngredientCasePipe
  ],
  templateUrl: './ingredient-editor.component.html',
  styleUrl: './ingredient-editor.component.scss'
})
export class IngredientEditorComponent implements OnInit, OnDestroy {
  private readonly api = inject(RecipeApiService);
  readonly unitsService = inject(UnitsService);
  private readonly destroy$ = new Subject<void>();

  @Input({ required: true }) ingredientGroup!: FormGroup;
  /** @deprecated Units are now sourced from UnitsService. This input is kept for API compatibility
   *  with stage-editor but is not used. */
  @Input() units: UnitOptionDto[] = [];
  @Output() removed = new EventEmitter<void>();

  /** Standalone display control drives the typeahead search.
   *  Its value is the visible text only — the actual form value
   *  is stored in ingredientGroup.ingredientId (INT). */
  readonly displayControl = new FormControl<string>('');

  suggestions: SuggestionItem[] = [];
  isCreating = false;

  // ─── Inline add-unit form state ──────────────────────────────────────────
  showAddUnitForm = false;
  isCreatingUnit = false;
  unitNameError = '';

  readonly unitNameControl = new FormControl<string>('', [Validators.required]);
  readonly unitAbbreviationControl = new FormControl<string>('');

  ngOnInit(): void {
    // Restore display text when editing an existing recipe.
    const existingName: string = this.ingredientGroup.get('ingredientDisplayName')?.value ?? '';
    if (existingName) {
      this.displayControl.setValue(existingName, { emitEvent: false });
    }

    this.displayControl.valueChanges.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(term => {
      const trimmed = (term ?? '').trim();

      // Clear resolved ingredient if user edits the text after a selection.
      if (trimmed !== (this.ingredientGroup.get('ingredientDisplayName')?.value ?? '')) {
        this.ingredientGroup.get('ingredientId')?.setValue(null);
        this.ingredientGroup.get('ingredientDisplayName')?.setValue('');
      }

      if (trimmed.length < 2) {
        this.suggestions = [];
        return;
      }

      this.api.searchIngredients(trimmed).pipe(
        takeUntil(this.destroy$)
      ).subscribe(results => {
        this.suggestions = results.map((r): SuggestionItem => ({
          id: r.id,
          name: r.name,
          isAddNew: false
        }));

        // Append "Add new" option — always shown so users can create missing ingredients.
        this.suggestions.push({
          id: ADD_NEW_SENTINEL,
          name: trimmed,
          isAddNew: true
        });
      });
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onOptionSelected(event: MatAutocompleteSelectedEvent): void {
    const selected = event.option.value as SuggestionItem;

    if (selected.isAddNew) {
      this.createAndSelect(selected.name);
    } else {
      this.resolveSelection(selected.id as number, selected.name);
    }
  }

  /** displayWith function for mat-autocomplete — keeps the input showing
   *  the ingredient name text after a selection rather than [object Object]. */
  displayFn = (item: SuggestionItem | string | null): string => {
    if (!item) return '';
    if (typeof item === 'string') return item;
    return item.name;
  };

  // ─── Inline add-unit ─────────────────────────────────────────────────────

  /** Called when user selects the native <select> — intercepts the sentinel value. */
  onUnitSelectChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    if (select.value === '__ADD_UNIT__') {
      // Revert both the DOM element and the form control back to the previous value,
      // so the reactive form doesn't store the sentinel string.
      const unitControl = this.ingredientGroup.get('unitId');
      const previousValue = unitControl?.value ?? '';
      select.value = previousValue;
      unitControl?.setValue(previousValue, { emitEvent: false });
      this.openAddUnitForm();
    }
  }

  openAddUnitForm(): void {
    this.showAddUnitForm = true;
    this.unitNameControl.reset('');
    this.unitAbbreviationControl.reset('');
    this.unitNameError = '';
  }

  cancelAddUnit(): void {
    this.showAddUnitForm = false;
    this.unitNameError = '';
  }

  confirmAddUnit(): void {
    if (this.unitNameControl.invalid || this.isCreatingUnit) return;

    const name = (this.unitNameControl.value ?? '').trim();
    const abbreviation = (this.unitAbbreviationControl.value ?? '').trim() || undefined;

    if (!name) {
      this.unitNameControl.setErrors({ required: true });
      return;
    }

    this.isCreatingUnit = true;
    this.unitNameError = '';

    this.unitsService.createUnit(name, abbreviation).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (created: UnitOptionDto) => {
        this.isCreatingUnit = false;
        this.showAddUnitForm = false;
        // Select the newly created unit in this row.
        this.ingredientGroup.get('unitId')?.setValue(created.id);
      },
      error: (err) => {
        this.isCreatingUnit = false;
        if (err?.status === 409) {
          this.unitNameError = 'A unit with this name already exists';
        } else {
          this.unitNameError = 'Failed to create unit — please try again';
        }
      }
    });
  }

  private resolveSelection(id: number, name: string): void {
    this.ingredientGroup.get('ingredientId')?.setValue(id);
    this.ingredientGroup.get('ingredientDisplayName')?.setValue(name);
    this.displayControl.setValue(name, { emitEvent: false });
    this.suggestions = [];
  }

  private createAndSelect(name: string): void {
    if (this.isCreating) return;
    this.isCreating = true;

    this.api.createIngredient(name).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (created: IngredientOptionDto) => {
        this.isCreating = false;
        this.resolveSelection(created.id, created.name);
      },
      error: () => {
        // On 409 (duplicate), search for the existing ingredient and resolve.
        this.api.searchIngredients(name).pipe(
          takeUntil(this.destroy$)
        ).subscribe(results => {
          this.isCreating = false;
          const normalised = name.trim().toLowerCase();
          const match = results.find(r => r.name === normalised) ?? results[0];
          if (match) {
            this.resolveSelection(match.id, match.name);
          }
        });
      }
    });
  }
}
