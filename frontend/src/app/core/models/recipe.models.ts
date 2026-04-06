/* ---- Review DTOs (for RatingReviewComponent / WAL-14) ---- */

export interface ReviewDto {
  id: number;
  recipeId: number;
  userId: number;
  userName: string;
  rating: number;
  notes: string | null;
  madeOn: string | null;
  createdAt: string;
}

export interface CreateReviewPayload {
  userId: number;
  rating: number;
  notes: string | null;
  madeOn: string | null;
}

/* ---- User DTOs ---- */

export interface UserDto {
  id: number;
  name: string;
  themeName: string;
}

/* ---- Reference data DTOs ---- */

export interface IngredientOptionDto {
  id: number;
  name: string;
}

export interface UnitOptionDto {
  id: number;
  name: string;
  abbreviation: string | null;
  unitType: string | null;
}

export interface TagOptionDto {
  id: number;
  name: string;
  slug: string;
  categoryId: number;
  categoryName: string;
}

/* ---- Form payload DTOs ---- */

export interface RecipeIngredientPayload {
  ingredientName: string;
  amount: number | null;
  unitId: number | null;
  notes: string | null;
}

export interface RecipeStepPayload {
  instruction: string;
}

export interface RecipeStagePayload {
  name: string;
  description: string | null;
  steps: RecipeStepPayload[];
  ingredients: RecipeIngredientPayload[];
}

export interface SaveRecipePayload {
  title: string;
  description: string | null;
  source: string | null;
  imageUrl: string | null;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  servings: number | null;
  tagIds: number[];
  stages: RecipeStagePayload[];
}

export type ImageStyle = 'rustic' | 'minimalist' | 'mediterranean' | 'cosy' | 'classic' | 'moody';

export interface GenerateImagePayload {
  description: string;
  ingredients: string[];
  style: ImageStyle;
  freeText?: string;
}

export interface IdealiseImagePayload {
  style: ImageStyle;
  freeText?: string;
}

/* ---- List view DTOs ---- */

export interface TagDto {
  id: number;
  name: string;
  categoryName: string;
}

export interface RatingDto {
  userId: number;
  userName: string;
  averageRating: number;
}

export interface RecipeSummaryDto {
  id: number;
  title: string;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  imageUrl: string | null;
  tags: TagDto[];
  ratings: RatingDto[];
}

/* ---- Detail view DTOs (matches backend RecipeDetailDto) ---- */

export interface RecipeDetailIngredientDto {
  id: number;
  ingredientId: number;
  ingredientName: string;
  amount: string | null;
  unitId: number | null;
  unitName: string | null;
  unitAbbreviation: string | null;
  notes: string | null;
  weightGrams: number | null;
  sortOrder: number;
}

export interface RecipeDetailStepDto {
  id: number;
  instruction: string;
  sortOrder: number;
}

export interface RecipeDetailStageDto {
  id: number;
  name: string;
  description: string | null;
  sortOrder: number;
  steps: RecipeDetailStepDto[];
  ingredients: RecipeDetailIngredientDto[];
}

export interface RecipeDetailTagDto {
  id: number;
  name: string;
  categoryName: string;
}

export interface RecipeDetailReviewDto {
  id: number;
  userId: number;
  userName: string;
  rating: number;
  notes: string | null;
  madeOn: string | null;
  createdAt: string;
}

export interface RecipeDetailDto {
  id: number;
  title: string;
  description: string | null;
  source: string | null;
  imageUrl: string | null;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  servings: number | null;
  createdAt: string;
  updatedAt: string;
  stages: RecipeDetailStageDto[];
  tags: RecipeDetailTagDto[];
  reviews: RecipeDetailReviewDto[];
}
