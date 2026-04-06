import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RecipeDetailIngredientDto } from '../../../core/models/recipe.models';
import { IngredientCasePipe } from '../../../shared/pipes/ingredient-case.pipe';

@Component({
  selector: 'app-ingredient-list',
  standalone: true,
  imports: [CommonModule, IngredientCasePipe],
  templateUrl: './ingredient-list.component.html',
  styleUrl: './ingredient-list.component.scss'
})
export class IngredientListComponent {
  @Input({ required: true }) ingredients!: RecipeDetailIngredientDto[];

  /** In multi-stage mode a sub-heading is shown above the list */
  @Input() stageName: string | null = null;

  private readonly checked = new Set<number>();

  isChecked(id: number): boolean {
    return this.checked.has(id);
  }

  toggle(id: number): void {
    if (this.checked.has(id)) {
      this.checked.delete(id);
    } else {
      this.checked.add(id);
    }
  }

  formatAmount(ingredient: RecipeDetailIngredientDto): string {
    const parts: string[] = [];
    if (ingredient.amount) parts.push(ingredient.amount);
    if (ingredient.unitAbbreviation) parts.push(ingredient.unitAbbreviation);
    return parts.join('\u00a0'); // non-breaking space
  }
}
