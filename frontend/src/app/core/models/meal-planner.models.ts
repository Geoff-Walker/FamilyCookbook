// ---------------------------------------------------------------------------
// Meal Planner DTOs (WAL-78)
// ---------------------------------------------------------------------------

export type SlotType = 'recipe' | 'if_its' | 'not_defined';

export interface MealPlanSlotDto {
  id: number;
  slotDate: string;         // ISO date string e.g. "2026-04-15"
  slotType: SlotType;
  recipeId: number | null;
  recipeName: string | null;
  batchMultiplier: number;
  notes: string | null;
  sortOrder: number;
}

export interface CreateMealPlanSlotPayload {
  slotDate: string;         // ISO date string
  slotType: SlotType;
  recipeId: number | null;
  batchMultiplier: number;
  notes: string | null;
  sortOrder: number;
}
