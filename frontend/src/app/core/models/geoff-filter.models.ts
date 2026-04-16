export type SuggestionStatus = 'pending' | 'backlogged' | 'accepted' | 'deleted';

export interface RecipeSuggestionDto {
  id: number;
  suggestedBy: number;
  suggestedByName: string;
  suggestionUrl: string | null;
  suggestionText: string | null;
  status: SuggestionStatus;
  createdAt: string; // ISO 8601
  recipeId: number | null;
  recipeName: string | null;
}

export interface CreateSuggestionPayload {
  suggestedBy: number;
  suggestionUrl: string | null;
  suggestionText: string | null;
}

export interface AcceptSuggestionPayload {
  requestingUserId: number;
}

export interface AcceptSuggestionResponse {
  suggestionId: number;
  status: SuggestionStatus;
  recipeId: number;
  recipeName: string;
}
