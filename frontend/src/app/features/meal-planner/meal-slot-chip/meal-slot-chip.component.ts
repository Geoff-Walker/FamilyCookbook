import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MealPlanSlotDto } from '../../../core/models/meal-planner.models';

/**
 * Colour-coded chip representing a single meal plan slot inside a day cell.
 * Emits (deleted) when the × button is tapped.
 */
@Component({
  selector: 'app-meal-slot-chip',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './meal-slot-chip.component.html',
  styleUrl: './meal-slot-chip.component.scss'
})
export class MealSlotChipComponent {
  @Input({ required: true }) slot!: MealPlanSlotDto;

  /** Emitted with the slot id when the user taps the delete (×) button. */
  @Output() deleted = new EventEmitter<number>();

  get label(): string {
    if (this.slot.slotType === 'recipe') {
      const name = this.slot.recipeName ?? 'Recipe';
      return this.slot.batchMultiplier > 1 ? `${name} ×${this.slot.batchMultiplier}` : name;
    }
    if (this.slot.slotType === 'if_its') {
      return this.slot.notes ?? 'If it\'s…';
    }
    return this.slot.notes ?? 'TBD';
  }

  onDelete(event: MouseEvent): void {
    event.stopPropagation();
    this.deleted.emit(this.slot.id);
  }
}
