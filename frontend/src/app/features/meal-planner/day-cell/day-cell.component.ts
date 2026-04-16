import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MealPlanSlotDto } from '../../../core/models/meal-planner.models';
import { MealSlotChipComponent } from '../meal-slot-chip/meal-slot-chip.component';

export interface AddRecipeEvent {
  date: string; // ISO date string
}

export interface AddIfItsEvent {
  date: string;
}

export interface AddTbdEvent {
  date: string;
}

export interface DeleteSlotEvent {
  slotId: number;
}

/**
 * A single day cell in the calendar grid.
 * Renders slot chips and add buttons.
 * Emits events up for the parent (CalendarGridComponent) to handle API calls.
 */
@Component({
  selector: 'app-day-cell',
  standalone: true,
  imports: [CommonModule, MatIconModule, MealSlotChipComponent],
  templateUrl: './day-cell.component.html',
  styleUrl: './day-cell.component.scss'
})
export class DayCellComponent implements OnChanges {
  /** ISO date string for this cell e.g. "2026-04-15". */
  @Input({ required: true }) date!: string;

  /** Slots that fall on this date. */
  @Input() slots: MealPlanSlotDto[] = [];

  /** Whether this date is in the currently displayed month. */
  @Input() isCurrentMonth = true;

  /** Whether this date is today. */
  @Input() isToday = false;

  /** Emitted when the user taps "+ Recipe". */
  @Output() addRecipe = new EventEmitter<AddRecipeEvent>();

  /** Emitted when the user confirms an "if it's" text entry. */
  @Output() addIfIts = new EventEmitter<AddIfItsEvent>();

  /** Emitted when the user taps "+ TBD". */
  @Output() addTbd = new EventEmitter<AddTbdEvent>();

  /** Emitted when the user taps the × on a chip. */
  @Output() deleteSlot = new EventEmitter<DeleteSlotEvent>();

  /** Display day number (1–31). */
  dayNumber = 0;

  /** Full mobile day label e.g. "Monday 14 April" (AC8). */
  mobileDayLabel = '';

  private static readonly WEEK_DAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  private static readonly MONTHS = ['January', 'February', 'March', 'April', 'May', 'June',
    'July', 'August', 'September', 'October', 'November', 'December'];

  ngOnChanges(): void {
    if (this.date) {
      const d = new Date(this.date + 'T00:00:00');
      this.dayNumber = d.getDate();
      this.mobileDayLabel = `${DayCellComponent.WEEK_DAYS[d.getDay()]} ${d.getDate()} ${DayCellComponent.MONTHS[d.getMonth()]}`;
    }
  }

  onAddRecipe(): void {
    this.addRecipe.emit({ date: this.date });
  }

  onAddIfIts(): void {
    // "If it's" is a slot category — emit immediately, no inline input.
    this.addIfIts.emit({ date: this.date });
  }

  onAddTbd(): void {
    this.addTbd.emit({ date: this.date });
  }

  onDeleteSlot(slotId: number): void {
    this.deleteSlot.emit({ slotId });
  }
}
