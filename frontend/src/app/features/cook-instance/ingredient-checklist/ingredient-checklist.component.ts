import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CookInstanceIngredientDto,
  CookInstanceStageGroupDto
} from '../../../core/models/cook-instance.models';
import { LimiterInputComponent } from '../limiter-input/limiter-input.component';

export interface IngredientPatchEvent {
  ingredientId: number;
  patch: { checked?: boolean; amount?: number; isLimiter?: boolean };
}

/** Emitted when the user removes an ingredient from the cook instance. */
export interface IngredientRemoveEvent {
  /** CookInstanceIngredient.Id */
  ingredientId: number;
}

interface RowState {
  ingredient: CookInstanceIngredientDto;
  /** Current displayed amount (base or scaled). */
  displayAmount: number;
  /** Whether the user has manually edited the amount field. */
  manuallyOverridden: boolean;
  /** The value in the amount field as a string (for input binding). */
  amountFieldValue: string;
  /** Limiter's "qty I have" value — only relevant when isLimiter is true. */
  limiterQty: number | null;
}

@Component({
  selector: 'app-ingredient-checklist',
  standalone: true,
  imports: [CommonModule, FormsModule, LimiterInputComponent],
  templateUrl: './ingredient-checklist.component.html',
  styleUrl: './ingredient-checklist.component.scss'
})
export class IngredientChecklistComponent implements OnChanges {
  @Input() stageGroups: CookInstanceStageGroupDto[] = [];
  @Input() basePortions: number | null = null;

  /** Emitted whenever a row needs a PATCH call. */
  @Output() ingredientPatched = new EventEmitter<IngredientPatchEvent>();

  /** Emitted when the user removes an ingredient entirely from the cook. */
  @Output() ingredientRemoved = new EventEmitter<IngredientRemoveEvent>();

  /** Flat map of row states, keyed by cook instance ingredient ID. */
  rowStates = new Map<number, RowState>();

  /** Whether multi-stage headers should be shown. */
  get isMultiStage(): boolean {
    return this.stageGroups.length > 1 ||
      (this.stageGroups.length === 1 && this.stageGroups[0].stageName !== null);
  }

  /** Current binding scale factor (1.0 = no scaling). */
  get scaleFactor(): number {
    const limiters = this.activeLimiters;
    if (limiters.length === 0) return 1;

    const factors = limiters.map(s => {
      if (!s.limiterQty || s.limiterQty <= 0 || s.ingredient.amount <= 0) return 1;
      return s.limiterQty / s.ingredient.amount;
    });

    return Math.min(...factors);
  }

  get activeLimiters(): RowState[] {
    const result: RowState[] = [];
    for (const state of this.rowStates.values()) {
      if (state.ingredient.isLimiter) result.push(state);
    }
    return result;
  }

  get limiterCount(): number {
    return this.activeLimiters.length;
  }

  /** Scaled portions to display in callout. */
  get scaledPortions(): number | null {
    if (this.basePortions == null) return null;
    return Math.round(this.basePortions * this.scaleFactor * 10) / 10;
  }

  /** Binding limiter (lowest scale factor when two are set). */
  get bindingLimiter(): RowState | null {
    const limiters = this.activeLimiters;
    if (limiters.length === 0) return null;
    if (limiters.length === 1) return limiters[0];

    let binding = limiters[0];
    let minFactor = this.limiterScaleFactor(limiters[0]);
    for (const l of limiters.slice(1)) {
      const f = this.limiterScaleFactor(l);
      if (f < minFactor) {
        minFactor = f;
        binding = l;
      }
    }
    return binding;
  }

  limiterScaleFactor(state: RowState): number {
    if (!state.limiterQty || state.limiterQty <= 0 || state.ingredient.amount <= 0) return 1;
    return state.limiterQty / state.ingredient.amount;
  }

  limiterPortions(state: RowState): number | null {
    if (this.basePortions == null) return null;
    const f = this.limiterScaleFactor(state);
    return Math.round(this.basePortions * f * 10) / 10;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['stageGroups']) {
      this.initRowStates();
    }
  }

  private initRowStates(): void {
    // Preserve existing manual overrides if the ingredient ID is already tracked.
    for (const stage of this.stageGroups) {
      for (const ing of stage.ingredients) {
        if (!this.rowStates.has(ing.id)) {
          this.rowStates.set(ing.id, {
            ingredient: ing,
            displayAmount: ing.amount,
            manuallyOverridden: false,
            amountFieldValue: this.formatAmount(ing.amount),
            limiterQty: null
          });
        }
      }
    }
  }

  // -------------------------------------------------------------------------
  // Tick / untick
  // -------------------------------------------------------------------------

  onToggleChecked(ingredientId: number): void {
    const state = this.rowStates.get(ingredientId);
    if (!state) return;
    state.ingredient.checked = !state.ingredient.checked;
    this.ingredientPatched.emit({
      ingredientId,
      patch: { checked: state.ingredient.checked }
    });
  }

  // -------------------------------------------------------------------------
  // Amount field
  // -------------------------------------------------------------------------

  onAmountInput(ingredientId: number, value: string): void {
    const state = this.rowStates.get(ingredientId);
    if (!state) return;
    state.amountFieldValue = value;
    const parsed = parseFloat(value);
    if (!isNaN(parsed) && parsed !== state.ingredient.amount) {
      state.manuallyOverridden = true;
      state.displayAmount = parsed;
    }
  }

  onAmountBlur(ingredientId: number): void {
    const state = this.rowStates.get(ingredientId);
    if (!state) return;
    const parsed = parseFloat(state.amountFieldValue);
    console.log('[CookChecklist] blur', {
      ingredientId,
      amountFieldValue: state.amountFieldValue,
      parsed,
      stateIngredientAmount: state.ingredient.amount,
      typeofStateAmount: typeof state.ingredient.amount,
      willPatch: !isNaN(parsed) && parsed !== state.ingredient.amount
    });
    if (isNaN(parsed)) {
      // Revert to current display amount
      state.amountFieldValue = this.formatAmount(state.displayAmount);
      return;
    }
    if (parsed !== state.ingredient.amount) {
      this.ingredientPatched.emit({ ingredientId, patch: { amount: parsed } });
    }
  }

  isAmountOverridden(ingredientId: number): boolean {
    const state = this.rowStates.get(ingredientId);
    return state?.manuallyOverridden ?? false;
  }

  // -------------------------------------------------------------------------
  // Limiter toggle
  // -------------------------------------------------------------------------

  onToggleLimiter(ingredientId: number): void {
    const state = this.rowStates.get(ingredientId);
    if (!state) return;

    const currentlyLimiter = state.ingredient.isLimiter;

    // Enforce max 2 limiters — silently ignore if already at 2 and trying to add a third.
    if (!currentlyLimiter && this.limiterCount >= 2) return;

    const newIsLimiter = !currentlyLimiter;
    state.ingredient.isLimiter = newIsLimiter;

    if (!newIsLimiter) {
      // Turning off — clear qty and rescale
      state.limiterQty = null;
      this.rescaleAll();
    }

    this.ingredientPatched.emit({ ingredientId, patch: { isLimiter: newIsLimiter } });
  }

  onLimiterQuantityChange(ingredientId: number, qty: number | null): void {
    const state = this.rowStates.get(ingredientId);
    if (!state) return;
    state.limiterQty = qty;
    this.rescaleAll();
  }

  // -------------------------------------------------------------------------
  // Flush scaled amounts
  // -------------------------------------------------------------------------

  /**
   * Emits ingredientPatched events for any ingredient whose displayed amount
   * differs from its stored amount (i.e. scaled by limiter but not yet PATCHed).
   * Called by the parent before opening the complete-cook review dialog so
   * that scaled quantities are persisted before the cook is finalised.
   */
  flushScaledAmounts(): void {
    for (const [ingredientId, state] of this.rowStates.entries()) {
      const displayed = Math.round(state.displayAmount * 1000) / 1000;
      const stored = Math.round(state.ingredient.amount * 1000) / 1000;
      if (displayed !== stored) {
        this.ingredientPatched.emit({ ingredientId, patch: { amount: state.displayAmount } });
      }
    }
  }

  // -------------------------------------------------------------------------
  // Remove ingredient
  // -------------------------------------------------------------------------

  onRemoveIngredient(ingredientId: number): void {
    // Remove from rowStates immediately so rescaling ignores it
    this.rowStates.delete(ingredientId);
    this.ingredientRemoved.emit({ ingredientId });
  }

  // -------------------------------------------------------------------------
  // Rescaling
  // -------------------------------------------------------------------------

  private rescaleAll(): void {
    const factor = this.scaleFactor;
    for (const state of this.rowStates.values()) {
      if (!state.manuallyOverridden) {
        const newAmount = state.ingredient.amount * factor;
        state.displayAmount = newAmount;
        state.amountFieldValue = this.formatAmount(newAmount);
      }
    }
  }

  // -------------------------------------------------------------------------
  // Display helpers
  // -------------------------------------------------------------------------

  formatAmount(amount: number): string {
    if (amount === Math.floor(amount)) return amount.toString();
    return (Math.round(amount * 100) / 100).toString();
  }

  getRowState(ingredientId: number): RowState | undefined {
    return this.rowStates.get(ingredientId);
  }

  trackByIngredientId(_: number, ing: CookInstanceIngredientDto): number {
    return ing.id;
  }

  trackByStage(_: number, stage: CookInstanceStageGroupDto): number | null {
    return stage.stageId;
  }
}
