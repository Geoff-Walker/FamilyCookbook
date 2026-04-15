import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { MealPlannerApiService } from '../../core/services/meal-planner-api.service';
import { MealPlanSlotDto } from '../../core/models/meal-planner.models';
import { CalendarGridComponent } from './calendar-grid/calendar-grid.component';
import { AddToPlannerDialogComponent } from './add-to-planner-dialog/add-to-planner-dialog.component';
import {
  AddRecipeEvent,
  AddIfItsEvent,
  AddTbdEvent,
  DeleteSlotEvent
} from './day-cell/day-cell.component';

type TabId = 'full-calendar' | 'shopping-week';

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
];

/**
 * MealPlannerPageComponent — routed at /meal-planner.
 * Full calendar view with month navigation, slot loading, and add/delete actions.
 */
@Component({
  selector: 'app-meal-planner',
  standalone: true,
  imports: [CommonModule, CalendarGridComponent, AddToPlannerDialogComponent],
  templateUrl: './meal-planner.component.html',
  styleUrl: './meal-planner.component.scss'
})
export class MealPlannerComponent implements OnInit, OnDestroy {
  activeTab: TabId = 'full-calendar';

  /** Currently displayed year and month (1-indexed). */
  displayYear = new Date().getFullYear();
  displayMonth = new Date().getMonth() + 1;

  /** Slots loaded for the current month view. */
  slots: MealPlanSlotDto[] = [];

  isLoading = false;
  loadError: string | null = null;

  /** Controls visibility of the Add to Planner dialog. */
  showAddDialog = false;

  /** The ISO date string passed to the dialog when opened from a day cell. */
  dialogDate: string | null = null;

  private readonly destroy$ = new Subject<void>();

  constructor(private readonly mealPlannerApi: MealPlannerApiService) {}

  ngOnInit(): void {
    this.loadMonth();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get monthLabel(): string {
    return `${MONTH_NAMES[this.displayMonth - 1]} ${this.displayYear}`;
  }

  // ---------------------------------------------------------------------------
  // Month navigation
  // ---------------------------------------------------------------------------

  previousMonth(): void {
    if (this.displayMonth === 1) {
      this.displayMonth = 12;
      this.displayYear--;
    } else {
      this.displayMonth--;
    }
    this.loadMonth();
  }

  nextMonth(): void {
    if (this.displayMonth === 12) {
      this.displayMonth = 1;
      this.displayYear++;
    } else {
      this.displayMonth++;
    }
    this.loadMonth();
  }

  // ---------------------------------------------------------------------------
  // Data loading
  // ---------------------------------------------------------------------------

  loadMonth(): void {
    this.isLoading = true;
    this.loadError = null;

    const from = this.firstDayOfMonth();
    const to = this.lastDayOfMonth();

    this.mealPlannerApi.getSlots(from, to)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (slots) => {
          this.slots = slots;
          this.isLoading = false;
        },
        error: () => {
          this.loadError = 'Failed to load meal plan. Please try again.';
          this.isLoading = false;
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Day cell event handlers
  // ---------------------------------------------------------------------------

  onAddRecipe(event: AddRecipeEvent): void {
    this.dialogDate = event.date;
    this.showAddDialog = true;
  }

  onAddIfIts(event: AddIfItsEvent): void {
    this.mealPlannerApi.createSlot({
      slotDate: event.date,
      slotType: 'if_its',
      recipeId: null,
      batchMultiplier: 1,
      notes: null,
      sortOrder: 0
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (slot) => this.addSlotLocally(slot),
      error: () => { /* silently ignore — user sees no chip, can retry */ }
    });
  }

  onAddTbd(event: AddTbdEvent): void {
    this.mealPlannerApi.createSlot({
      slotDate: event.date,
      slotType: 'not_defined',
      recipeId: null,
      batchMultiplier: 1,
      notes: null,
      sortOrder: 0
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (slot) => this.addSlotLocally(slot),
      error: () => { /* silently ignore */ }
    });
  }

  onDeleteSlot(event: DeleteSlotEvent): void {
    // Optimistic removal — remove locally immediately, then call API
    this.slots = this.slots.filter(s => s.id !== event.slotId);
    this.mealPlannerApi.deleteSlot(event.slotId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        error: () => {
          // On failure reload the month to restore state
          this.loadMonth();
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Dialog events
  // ---------------------------------------------------------------------------

  onSlotAdded(slot: MealPlanSlotDto): void {
    this.addSlotLocally(slot);
    this.showAddDialog = false;
    this.dialogDate = null;
  }

  onDialogDismissed(): void {
    this.showAddDialog = false;
    this.dialogDate = null;
  }

  // ---------------------------------------------------------------------------
  // Tab
  // ---------------------------------------------------------------------------

  setTab(tab: TabId): void {
    this.activeTab = tab;
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  private addSlotLocally(slot: MealPlanSlotDto): void {
    this.slots = [...this.slots, slot];
  }

  private firstDayOfMonth(): string {
    return `${this.displayYear}-${String(this.displayMonth).padStart(2, '0')}-01`;
  }

  private lastDayOfMonth(): string {
    const last = new Date(this.displayYear, this.displayMonth, 0).getDate();
    return `${this.displayYear}-${String(this.displayMonth).padStart(2, '0')}-${String(last).padStart(2, '0')}`;
  }
}
