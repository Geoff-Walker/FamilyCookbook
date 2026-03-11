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
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  servings: number | null;
  createdAt: string;
  updatedAt: string;
  stages: RecipeDetailStageDto[];
  tags: RecipeDetailTagDto[];
  reviews: RecipeDetailReviewDto[];
}
