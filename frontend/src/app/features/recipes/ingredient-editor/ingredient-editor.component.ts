import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
  inject
} from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { IngredientOptionDto, UnitOptionDto } from '../../../core/models/recipe.models';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
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
  private readonly destroy$ = new Subject<void>();

  @Input({ required: true }) ingredientGroup!: FormGroup;
  @Input() units: UnitOptionDto[] = [];
  @Output() removed = new EventEmitter<void>();

  /** Standalone display control drives the typeahead search.
   *  Its value is the visible text only — the actual form value
   *  is stored in ingredientGroup.ingredientId (INT). */
  readonly displayControl = new FormControl<string>('');

  suggestions: SuggestionItem[] = [];
  isCreating = false;

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
