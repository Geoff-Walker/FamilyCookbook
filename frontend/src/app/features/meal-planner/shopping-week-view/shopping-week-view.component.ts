import {
  Component,
  OnInit,
  OnDestroy,
  HostListener
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, forkJoin, of, takeUntil } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { MealPlannerApiService } from '../../../core/services/meal-planner-api.service';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { MealPlanSlotDto } from '../../../core/models/meal-planner.models';
import { RecipeDetailDto } from '../../../core/models/recipe.models';
import { DayCellComponent, AddRecipeEvent, AddIfItsEvent, AddTbdEvent, DeleteSlotEvent } from '../day-cell/day-cell.component';
import { AddToPlannerDialogComponent } from '../add-to-planner-dialog/add-to-planner-dialog.component';
import { ShoppingListSheetComponent, ShoppingItem } from '../shopping-list-sheet/shopping-list-sheet.component';

const SHORT_DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
const SHORT_MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

/** Format a Date as "Sat 12 Apr" */
function shortDate(d: Date): string {
  return `${SHORT_DAYS[d.getDay()]} ${d.getDate()} ${SHORT_MONTHS[d.getMonth()]}`;
}

/** Format an ISO date string as "Sat 12 Apr" */
function shortDateFromIso(iso: string): string {
  return shortDate(new Date(iso + 'T00:00:00'));
}

/** Return the ISO date string (YYYY-MM-DD) for a given Date. */
function toIso(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

/** Add N days to a Date, returning a new Date. */
function addDays(d: Date, n: number): Date {
  const r = new Date(d);
  r.setDate(r.getDate() + n);
  return r;
}

/** Find the most recent Saturday on or before today. */
function lastSaturday(from: Date): Date {
  const d = new Date(from);
  // getDay(): 0=Sun,6=Sat. We want day 6.
  const diff = (d.getDay() + 7 - 6) % 7;
  d.setDate(d.getDate() - diff);
  return d;
}

/**
 * ShoppingWeekViewComponent — Shopping Week tab of the Meal Planner page.
 * Configurable date window (default Sat–Sat), day cell grid, and ephemeral shopping list.
 */
@Component({
  selector: 'app-shopping-week-view',
  standalone: true,
  imports: [
    CommonModule,
    DayCellComponent,
    AddToPlannerDialogComponent,
    ShoppingListSheetComponent
  ],
  templateUrl: './shopping-week-view.component.html',
  styleUrl: './shopping-week-view.component.scss'
})
export class ShoppingWeekViewComponent implements OnInit, OnDestroy {
  // -------------------------------------------------------------------------
  // Window state
  // -------------------------------------------------------------------------

  /** Start date of the window (ISO string). */
  windowStart!: string;
  /** End date of the window (ISO string). */
  windowEnd!: string;

  // -------------------------------------------------------------------------
  // Data
  // -------------------------------------------------------------------------

  slots: MealPlanSlotDto[] = [];
  isLoading = false;
  loadError: string | null = null;

  /** Cache of recipe details fetched this session — keyed by recipeId. */
  private recipeCache = new Map<number, RecipeDetailDto>();

  // -------------------------------------------------------------------------
  // Dialog / sheet state
  // -------------------------------------------------------------------------

  showAddDialog = false;
  dialogDate: string | null = null;
  showShoppingList = false;
  shoppingItems: ShoppingItem[] = [];
  ifItsItems: string[] = [];
  isGenerating = false;

  // -------------------------------------------------------------------------
  // Date picker state
  // -------------------------------------------------------------------------

  /** Controls whether the start-edge date input is visible. */
  showStartPicker = false;
  /** Controls whether the end-edge date input is visible. */
  showEndPicker = false;

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly mealPlannerApi: MealPlannerApiService,
    private readonly recipeApi: RecipeApiService
  ) {}

  ngOnInit(): void {
    const today = new Date();
    const sat = lastSaturday(today);
    this.windowStart = toIso(sat);
    this.windowEnd = toIso(addDays(sat, 7));
    this.loadWindow();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // -------------------------------------------------------------------------
  // Window label
  // -------------------------------------------------------------------------

  get windowLabel(): string {
    const start = new Date(this.windowStart + 'T00:00:00');
    const end = new Date(this.windowEnd + 'T00:00:00');
    const days = this.windowDayCount;
    const base = `${shortDate(start)} – ${shortDate(end)}`;
    // Show day count only when it differs from the default 7-day window
    return days === 7 ? base : `${base} (${days} days)`;
  }

  /** Mobile: just the start month abbreviation e.g. "Apr" */
  get windowLabelMobile(): string {
    return new Date(this.windowStart + 'T00:00:00').toLocaleDateString('en-GB', { month: 'short' });
  }

  get windowDayCount(): number {
    const start = new Date(this.windowStart + 'T00:00:00');
    const end = new Date(this.windowEnd + 'T00:00:00');
    return Math.round((end.getTime() - start.getTime()) / 86400000) + 1;
  }

  /** All dates in the current window as ISO strings. */
  get windowDates(): string[] {
    const dates: string[] = [];
    const start = new Date(this.windowStart + 'T00:00:00');
    const end = new Date(this.windowEnd + 'T00:00:00');
    const cur = new Date(start);
    while (cur <= end) {
      dates.push(toIso(cur));
      cur.setDate(cur.getDate() + 1);
    }
    return dates;
  }

  get hasRecipeSlots(): boolean {
    return this.slots.some(s => s.slotType === 'recipe');
  }

  // -------------------------------------------------------------------------
  // Window navigation — week stepper
  // -------------------------------------------------------------------------

  stepWeekBack(): void {
    const start = new Date(this.windowStart + 'T00:00:00');
    const end = new Date(this.windowEnd + 'T00:00:00');
    this.windowStart = toIso(addDays(start, -7));
    this.windowEnd = toIso(addDays(end, -7));
    this.loadWindow();
  }

  stepWeekForward(): void {
    const start = new Date(this.windowStart + 'T00:00:00');
    const end = new Date(this.windowEnd + 'T00:00:00');
    this.windowStart = toIso(addDays(start, 7));
    this.windowEnd = toIso(addDays(end, 7));
    this.loadWindow();
  }

  // -------------------------------------------------------------------------
  // Edge controls — date pickers (design override: calendar icon, not ±1 buttons)
  // -------------------------------------------------------------------------

  toggleStartPicker(): void {
    this.showStartPicker = !this.showStartPicker;
    this.showEndPicker = false;
  }

  toggleEndPicker(): void {
    this.showEndPicker = !this.showEndPicker;
    this.showStartPicker = false;
  }

  onStartDateChange(value: string): void {
    if (!value) return;
    // Validate: new start must be at least 2 days before end
    const newStart = new Date(value + 'T00:00:00');
    const end = new Date(this.windowEnd + 'T00:00:00');
    const diff = Math.round((end.getTime() - newStart.getTime()) / 86400000);
    if (diff < 1) {
      // Would make window 1 day or less — reject
      return;
    }
    this.windowStart = value;
    this.showStartPicker = false;
    this.loadWindow();
  }

  onEndDateChange(value: string): void {
    if (!value) return;
    const start = new Date(this.windowStart + 'T00:00:00');
    const newEnd = new Date(value + 'T00:00:00');
    const diff = Math.round((newEnd.getTime() - start.getTime()) / 86400000);
    if (diff < 1) {
      return;
    }
    this.windowEnd = value;
    this.showEndPicker = false;
    this.loadWindow();
  }

  /** Close pickers when clicking outside. */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.sw-edge')) {
      this.showStartPicker = false;
      this.showEndPicker = false;
    }
  }

  // -------------------------------------------------------------------------
  // Data loading
  // -------------------------------------------------------------------------

  loadWindow(): void {
    this.isLoading = true;
    this.loadError = null;

    this.mealPlannerApi.getSlots(this.windowStart, this.windowEnd)
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

  // -------------------------------------------------------------------------
  // Day cell event handlers
  // -------------------------------------------------------------------------

  slotsForDate(date: string): MealPlanSlotDto[] {
    return this.slots.filter(s => s.slotDate === date);
  }

  isToday(date: string): boolean {
    return date === toIso(new Date());
  }

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
      next: (slot) => { this.slots = [...this.slots, slot]; },
      error: () => { /* silently ignore */ }
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
      next: (slot) => { this.slots = [...this.slots, slot]; },
      error: () => { /* silently ignore */ }
    });
  }

  onDeleteSlot(event: DeleteSlotEvent): void {
    this.slots = this.slots.filter(s => s.id !== event.slotId);
    this.mealPlannerApi.deleteSlot(event.slotId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ error: () => { this.loadWindow(); } });
  }

  onSlotAdded(slot: MealPlanSlotDto): void {
    this.slots = [...this.slots, slot];
    this.showAddDialog = false;
    this.dialogDate = null;
  }

  onDialogDismissed(): void {
    this.showAddDialog = false;
    this.dialogDate = null;
  }

  // -------------------------------------------------------------------------
  // Shopping list generation
  // -------------------------------------------------------------------------

  generateShoppingList(): void {
    const recipeSlots = this.slots.filter(s => s.slotType === 'recipe' && s.recipeId != null);
    if (recipeSlots.length === 0) return;

    const uniqueRecipeIds = [...new Set(recipeSlots.map(s => s.recipeId!))];

    // Determine which recipe IDs need fetching (not yet cached)
    const toFetch = uniqueRecipeIds.filter(id => !this.recipeCache.has(id));

    this.isGenerating = true;

    const fetches$ = toFetch.length > 0
      ? forkJoin(toFetch.map(id =>
          this.recipeApi.getRecipe(id).pipe(
            catchError(() => of(null))
          )
        ))
      : of([] as (RecipeDetailDto | null)[]);

    fetches$.pipe(takeUntil(this.destroy$)).subscribe(results => {
      // Populate cache with freshly-fetched recipes
      (results as (RecipeDetailDto | null)[]).forEach(recipe => {
        if (recipe) {
          this.recipeCache.set(recipe.id, recipe);
        }
      });

      this.shoppingItems = this.aggregateIngredients(recipeSlots);
      this.ifItsItems = this.slots
        .filter(s => s.slotType === 'if_its')
        .map(s => s.notes?.trim() || 'If it\'s…');
      this.isGenerating = false;
      this.showShoppingList = true;
    });
  }

  onShoppingListClosed(): void {
    this.showShoppingList = false;
    this.shoppingItems = [];
  }

  /**
   * Aggregate ingredients from all recipe slots in the window.
   * Groups by ingredientId + unitId. Different units for the same ingredient
   * are kept as separate line items.
   */
  private aggregateIngredients(recipeSlots: MealPlanSlotDto[]): ShoppingItem[] {
    // Key: `${ingredientId}:${unitId ?? 'none'}`
    const map = new Map<string, ShoppingItem>();

    for (const slot of recipeSlots) {
      const recipe = this.recipeCache.get(slot.recipeId!);
      if (!recipe) continue;

      const multiplier = slot.batchMultiplier ?? 1;

      for (const stage of recipe.stages) {
        for (const ing of stage.ingredients) {
          const amount = ing.amount != null ? parseFloat(ing.amount) * multiplier : null;
          const key = `${ing.ingredientId}:${ing.unitId ?? 'none'}`;

          if (map.has(key)) {
            const existing = map.get(key)!;
            if (amount != null && existing.totalAmount != null) {
              existing.totalAmount += amount;
            }
            // If either side has no amount, keep as null (unknown quantity)
          } else {
            map.set(key, {
              ingredientId: ing.ingredientId,
              ingredientName: ing.ingredientName,
              unitId: ing.unitId,
              unitName: ing.unitName,
              unitAbbreviation: ing.unitAbbreviation,
              totalAmount: amount
            });
          }
        }
      }
    }

    return Array.from(map.values()).sort((a, b) =>
      a.ingredientName.localeCompare(b.ingredientName)
    );
  }

  /** Short date range string for the shopping list header sub-heading. */
  get windowRangeLabel(): string {
    const start = new Date(this.windowStart + 'T00:00:00');
    const end = new Date(this.windowEnd + 'T00:00:00');
    // "Sat 12 – Sat 19 Apr" — omit month on start if same month
    if (start.getMonth() === end.getMonth()) {
      return `${SHORT_DAYS[start.getDay()]} ${start.getDate()} – ${SHORT_DAYS[end.getDay()]} ${end.getDate()} ${SHORT_MONTHS[end.getMonth()]}`;
    }
    return `${shortDate(start)} – ${shortDate(end)}`;
  }
}
