import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
  inject
} from '@angular/core';
import { ReactiveFormsModule, FormGroup } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, filter, switchMap, takeUntil } from 'rxjs';
import { IngredientOptionDto, UnitOptionDto } from '../../../core/models/recipe.models';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { IngredientCasePipe } from '../../../shared/pipes/ingredient-case.pipe';

@Component({
  selector: 'app-ingredient-editor',
  standalone: true,
  imports: [ReactiveFormsModule, IngredientCasePipe],
  templateUrl: './ingredient-editor.component.html',
  styleUrl: './ingredient-editor.component.scss'
})
export class IngredientEditorComponent implements OnInit, OnDestroy {
  private readonly api = inject(RecipeApiService);
  private readonly searchInput$ = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  @Input({ required: true }) ingredientGroup!: FormGroup;
  @Input() units: UnitOptionDto[] = [];
  @Output() removed = new EventEmitter<void>();

  suggestions: IngredientOptionDto[] = [];
  showDropdown = false;

  ngOnInit(): void {
    this.searchInput$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      filter(term => term.length >= 2),
      switchMap(term => this.api.searchIngredients(term)),
      takeUntil(this.destroy$)
    ).subscribe(results => {
      this.suggestions = results;
      this.showDropdown = results.length > 0;
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onNameInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInput$.next(value);
    if (value.length < 2) {
      this.showDropdown = false;
      this.suggestions = [];
    }
  }

  selectSuggestion(suggestion: IngredientOptionDto): void {
    this.ingredientGroup.get('ingredientName')?.setValue(suggestion.name);
    this.showDropdown = false;
    this.suggestions = [];
  }

  closeDropdown(): void {
    setTimeout(() => {
      this.showDropdown = false;
    }, 150);
  }
}
