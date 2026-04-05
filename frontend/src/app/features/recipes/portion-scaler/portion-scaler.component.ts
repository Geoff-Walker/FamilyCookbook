import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RecipeDetailIngredientDto } from '../../../core/models/recipe.models';

export interface ScaledIngredient {
  id: number;
  ingredientName: string;
  scaledAmount: string | null;
  unitAbbreviation: string | null;
  scaledWeightGrams: number | null;
  notes: string | null;
}

@Component({
  selector: 'app-portion-scaler',
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [DecimalPipe],
  templateUrl: './portion-scaler.component.html',
  styleUrl: './portion-scaler.component.scss'
})
export class PortionScalerComponent implements OnChanges {
  /** Base serving count from the recipe. If null/0, the component is hidden. */
  @Input({ required: true }) baseServings!: number | null;

  /** All ingredients across all stages, flattened by the parent. */
  @Input({ required: true }) ingredients!: RecipeDetailIngredientDto[];

  desiredServings: number | null = null;

  scaledIngredients: ScaledIngredient[] = [];

  constructor(private readonly decimalPipe: DecimalPipe) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['baseServings'] || changes['ingredients']) {
      // Reset desired to the base whenever inputs change
      this.desiredServings = this.baseServings ?? null;
      this.recalculate();
    }
  }

  get isVisible(): boolean {
    return !!this.baseServings && this.baseServings > 0;
  }

  onDesiredChange(): void {
    this.recalculate();
  }

  private recalculate(): void {
    const base = this.baseServings;
    const desired = this.desiredServings;

    if (!base || !desired || desired < 1) {
      this.scaledIngredients = this.ingredients.map(ing => this.toScaled(ing, null));
      return;
    }

    this.scaledIngredients = this.ingredients.map(ing => this.toScaled(ing, desired / base));
  }

  private toScaled(ing: RecipeDetailIngredientDto, ratio: number | null): ScaledIngredient {
    const rawAmount = ing.amount ? parseFloat(ing.amount) : null;

    let scaledAmount: string | null = null;
    if (rawAmount !== null && !isNaN(rawAmount) && ratio !== null) {
      const scaled = Math.round(rawAmount * ratio * 100) / 100;
      scaledAmount = this.decimalPipe.transform(scaled, '1.0-2') ?? String(scaled);
    } else {
      // No numeric amount — show as-is (AC7)
      scaledAmount = ing.amount;
    }

    let scaledWeightGrams: number | null = null;
    if (ing.weightGrams !== null && ratio !== null) {
      scaledWeightGrams = Math.round(ing.weightGrams * ratio * 100) / 100;
    }

    return {
      id: ing.id,
      ingredientName: ing.ingredientName,
      scaledAmount,
      unitAbbreviation: ing.unitAbbreviation,
      scaledWeightGrams,
      notes: ing.notes
    };
  }

  formatWeight(grams: number): string {
    const formatted = this.decimalPipe.transform(grams, '1.0-2') ?? String(grams);
    return `${formatted}g`;
  }
}
