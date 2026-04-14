import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CookInstanceIngredientDto, CookInstanceStageGroupDto } from '../../../core/models/cook-instance.models';
import { RecipeDetailIngredientDto, RecipeDetailStageDto } from '../../../core/models/recipe.models';

/** Flattened row for the comparison table. */
export interface ComparisonRow {
  checked: boolean;
  ingredientName: string;
  notes: string | null;
  /** Actual amount from the cook instance (numeric). */
  actualAmount: number;
  actualUnit: string | null;
  /** Base amount from the recipe (string — may be null if none set). */
  baseAmount: string | null;
  baseUnit: string | null;
  /** True when actual differs from base quantity. */
  isDifferent: boolean;
}

@Component({
  selector: 'app-ingredient-comparison-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ingredient-comparison-table.component.html',
  styleUrl: './ingredient-comparison-table.component.scss'
})
export class IngredientComparisonTableComponent implements OnChanges {
  /** Stage groups from the cook instance — carry the actual quantities. */
  @Input({ required: true }) stageGroups: CookInstanceStageGroupDto[] = [];
  /** Stages from the base recipe — carry the base quantities. */
  @Input({ required: true }) recipeStages: RecipeDetailStageDto[] = [];
  /** Column header for the base recipe column — reflects whether the recipe has been promoted. */
  @Input() baseLabel: string = 'Base recipe';

  rows: ComparisonRow[] = [];

  /** Build a lookup of ingredientId → base ingredient for quick matching. */
  private buildBaseLookup(stages: RecipeDetailStageDto[]): Map<number, RecipeDetailIngredientDto> {
    const map = new Map<number, RecipeDetailIngredientDto>();
    for (const stage of stages) {
      for (const ing of stage.ingredients) {
        map.set(ing.ingredientId, ing);
      }
    }
    return map;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['stageGroups'] || changes['recipeStages']) {
      this.buildRows();
    }
  }

  private buildRows(): void {
    const baseLookup = this.buildBaseLookup(this.recipeStages);
    const result: ComparisonRow[] = [];

    for (const stage of this.stageGroups) {
      for (const ing of stage.ingredients) {
        const base = baseLookup.get(ing.ingredientId);
        const baseAmount = base?.amount ?? null;
        const baseUnit = base?.unitAbbreviation ?? base?.unitName ?? null;

        // Compare: parse base amount string to number for comparison
        const baseNumeric = baseAmount != null ? parseFloat(baseAmount) : null;
        const isDifferent =
          baseNumeric !== null && !isNaN(baseNumeric) && ing.amount !== baseNumeric;

        result.push({
          checked: ing.checked,
          ingredientName: ing.ingredientName,
          notes: ing.notes,
          actualAmount: ing.amount,
          actualUnit: ing.unitAbbreviation ?? ing.unitName ?? null,
          baseAmount,
          baseUnit,
          isDifferent
        });
      }
    }

    this.rows = result;
  }

  formatAmount(amount: number): string {
    if (amount === Math.floor(amount)) return amount.toString();
    return (Math.round(amount * 100) / 100).toString();
  }

  trackByIndex(index: number): number {
    return index;
  }
}
