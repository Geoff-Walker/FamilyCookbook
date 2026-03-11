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
