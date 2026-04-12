// ---------------------------------------------------------------------------
// Cook Instance models — matches backend DTOs from WAL-71
// ---------------------------------------------------------------------------

export interface StartCookPayload {
  recipeId: number;
  userId: number;
  portions: number | null;
}

export interface CookInstanceIngredientDto {
  id: number;
  ingredientId: number;
  ingredientName: string;
  amount: number;
  unitId: number | null;
  unitName: string | null;
  unitAbbreviation: string | null;
  checked: boolean;
  isLimiter: boolean;
  notes: string | null;
}

export interface CookInstanceStageGroupDto {
  stageId: number | null;
  stageName: string | null;
  sortOrder: number;
  ingredients: CookInstanceIngredientDto[];
}

export interface CookInstanceReviewSummaryDto {
  userId: number;
  userName: string;
  rating: number;
  notes: string | null;
}

export interface CookInstanceDetailDto {
  id: number;
  recipeId: number;
  recipeTitle: string;
  userId: number;
  userName: string;
  startedAt: string;
  completedAt: string | null;
  portions: number | null;
  notes: string | null;
  stageGroups: CookInstanceStageGroupDto[];
  reviews: CookInstanceReviewSummaryDto[];
}

export interface PatchCookIngredientPayload {
  checked?: boolean;
  amount?: number;
  isLimiter?: boolean;
}

// ---------------------------------------------------------------------------
// POST /api/cook-instances/{id}/complete — request
// ---------------------------------------------------------------------------

export interface CookReviewPayload {
  userId: number;
  rating: number;
  notes: string | null;
}

export interface CompleteCookPayload {
  portions: number | null;
  reviews: CookReviewPayload[];
}

// ---------------------------------------------------------------------------
// GET /api/recipes/{recipeId}/cook-instances — history list items (WAL-74)
// ---------------------------------------------------------------------------

export interface CookInstanceReviewSummaryDto {
  userId: number;
  userName: string;
  rating: number;
  notes: string | null;
}

export interface CookInstanceSummaryDto {
  id: number;
  userId: number;
  userName: string;
  startedAt: string;
  completedAt: string | null;
  portions: number | null;
  notes: string | null;
  reviews: CookInstanceReviewSummaryDto[];
}
