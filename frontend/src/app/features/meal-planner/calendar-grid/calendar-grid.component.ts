import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MealPlanSlotDto } from '../../../core/models/meal-planner.models';
import { DayCellComponent, AddRecipeEvent, AddIfItsEvent, AddTbdEvent, DeleteSlotEvent } from '../day-cell/day-cell.component';

export interface CalendarDay {
  date: string;         // ISO date string
  isCurrentMonth: boolean;
  isToday: boolean;
  slots: MealPlanSlotDto[];
}

/** Day-of-week header labels, Mon–Sun. */
const DAY_HEADERS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

/**
 * 7-column CSS grid calendar for a single month.
 * Generates padding days from the previous/next month to fill the grid.
 */
@Component({
  selector: 'app-calendar-grid',
  standalone: true,
  imports: [CommonModule, DayCellComponent],
  templateUrl: './calendar-grid.component.html',
  styleUrl: './calendar-grid.component.scss'
})
export class CalendarGridComponent implements OnChanges {
  /** The month to display (1-indexed). */
  @Input({ required: true }) year!: number;
  @Input({ required: true }) month!: number;  // 1 = January

  /** All slots loaded for this month (may include padding day slots). */
  @Input() slots: MealPlanSlotDto[] = [];

  @Output() addRecipe = new EventEmitter<AddRecipeEvent>();
  @Output() addIfIts = new EventEmitter<AddIfItsEvent>();
  @Output() addTbd = new EventEmitter<AddTbdEvent>();
  @Output() deleteSlot = new EventEmitter<DeleteSlotEvent>();

  readonly dayHeaders = DAY_HEADERS;
  calendarDays: CalendarDay[] = [];

  private todayIso = this.toIso(new Date());

  ngOnChanges(): void {
    this.todayIso = this.toIso(new Date());
    this.buildGrid();
  }

  private buildGrid(): void {
    if (!this.year || !this.month) return;

    const days: CalendarDay[] = [];
    const slotMap = this.buildSlotMap();

    // First day of the month (JS months are 0-indexed)
    const firstOfMonth = new Date(this.year, this.month - 1, 1);
    // Last day of the month
    const lastOfMonth = new Date(this.year, this.month, 0);

    // getDay() returns 0=Sun, 1=Mon … 6=Sat; convert to Mon=0 … Sun=6
    const firstWeekday = (firstOfMonth.getDay() + 6) % 7; // 0=Mon
    const lastWeekday = (lastOfMonth.getDay() + 6) % 7;   // 0=Mon

    // Padding days from previous month
    for (let i = firstWeekday - 1; i >= 0; i--) {
      const d = new Date(this.year, this.month - 1, -i);
      const iso = this.toIso(d);
      days.push({ date: iso, isCurrentMonth: false, isToday: iso === this.todayIso, slots: slotMap[iso] ?? [] });
    }

    // Days of current month
    for (let d = 1; d <= lastOfMonth.getDate(); d++) {
      const date = new Date(this.year, this.month - 1, d);
      const iso = this.toIso(date);
      days.push({ date: iso, isCurrentMonth: true, isToday: iso === this.todayIso, slots: slotMap[iso] ?? [] });
    }

    // Padding days from next month to complete the last row
    const trailingDays = lastWeekday === 6 ? 0 : 6 - lastWeekday;
    for (let i = 1; i <= trailingDays; i++) {
      const d = new Date(this.year, this.month, i);
      const iso = this.toIso(d);
      days.push({ date: iso, isCurrentMonth: false, isToday: iso === this.todayIso, slots: slotMap[iso] ?? [] });
    }

    this.calendarDays = days;
  }

  /** Build a map of ISO date → slots for fast lookup. */
  private buildSlotMap(): Record<string, MealPlanSlotDto[]> {
    const map: Record<string, MealPlanSlotDto[]> = {};
    for (const slot of this.slots) {
      const key = slot.slotDate.substring(0, 10); // normalise to YYYY-MM-DD
      if (!map[key]) map[key] = [];
      map[key].push(slot);
    }
    return map;
  }

  /** Format a Date as YYYY-MM-DD without timezone conversion. */
  private toIso(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
