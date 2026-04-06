import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { HttpParams } from '@angular/common/http';
import {
  RecipeSummaryDto,
  RecipeDetailDto,
  IngredientOptionDto,
  UnitOptionDto,
  TagOptionDto,
  SaveRecipePayload,
  UserDto,
  ReviewDto,
  CreateReviewPayload,
  GenerateImagePayload,
  IdealiseImagePayload
} from '../models/recipe.models';

@Injectable({
  providedIn: 'root'
})
export class RecipeApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  getRecipes(): Observable<RecipeSummaryDto[]> {
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/recipes`);
  }

  getRecipe(id: number): Observable<RecipeDetailDto> {
    return this.http.get<RecipeDetailDto>(`${this.baseUrl}/recipes/${id}`);
  }

  createRecipe(dto: SaveRecipePayload): Observable<RecipeDetailDto> {
    return this.http.post<RecipeDetailDto>(`${this.baseUrl}/recipes`, dto);
  }

  updateRecipe(id: number, dto: SaveRecipePayload): Observable<RecipeDetailDto> {
    return this.http.put<RecipeDetailDto>(`${this.baseUrl}/recipes/${id}`, dto);
  }

  deleteRecipe(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/recipes/${id}`);
  }

  searchIngredients(term: string): Observable<IngredientOptionDto[]> {
    const params = new HttpParams().set('search', term);
    return this.http.get<IngredientOptionDto[]>(`${this.baseUrl}/ingredients`, { params });
  }

  createIngredient(name: string): Observable<IngredientOptionDto> {
    return this.http.post<IngredientOptionDto>(`${this.baseUrl}/ingredients`, { name });
  }

  deleteIngredient(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/ingredients/${id}`);
  }

  getAllIngredients(): Observable<IngredientOptionDto[]> {
    return this.http.get<IngredientOptionDto[]>(`${this.baseUrl}/ingredients`);
  }

  getUnits(): Observable<UnitOptionDto[]> {
    return this.http.get<UnitOptionDto[]>(`${this.baseUrl}/units`);
  }

  createUnit(name: string, abbreviation?: string): Observable<UnitOptionDto> {
    return this.http.post<UnitOptionDto>(`${this.baseUrl}/units`, { name, abbreviation });
  }

  getTags(): Observable<TagOptionDto[]> {
    return this.http.get<TagOptionDto[]>(`${this.baseUrl}/tags`);
  }

  getUsers(): Observable<UserDto[]> {
    return this.http.get<UserDto[]>(`${this.baseUrl}/users`);
  }

  search(query: string, userId: number): Observable<RecipeSummaryDto[]> {
    const params = new HttpParams()
      .set('query', query)
      .set('userId', userId.toString());
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/search`, { params });
  }

  /** Filter recipes by ingredient IDs — returns recipes containing ALL specified ingredients. */
  filterRecipes(ingredientIds: number[]): Observable<RecipeSummaryDto[]> {
    const params = new HttpParams().set('ingredientIds', ingredientIds.join(','));
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/recipes/filter`, { params });
  }

  /** Filter recipes by tag IDs — AND across categories, OR within same category (AC9). */
  filterByTags(tagIds: number[]): Observable<RecipeSummaryDto[]> {
    const params = new HttpParams().set('tagIds', tagIds.join(','));
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/recipes/filter`, { params });
  }

  /** Filter recipes by ingredient IDs AND tag IDs combined. */
  filterByIngredientsAndTags(ingredientIds: number[], tagIds: number[]): Observable<RecipeSummaryDto[]> {
    let params = new HttpParams();
    if (ingredientIds.length > 0) params = params.set('ingredientIds', ingredientIds.join(','));
    if (tagIds.length > 0) params = params.set('tagIds', tagIds.join(','));
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/recipes/filter`, { params });
  }

  /** Combined semantic search + ingredient filter. */
  searchWithIngredients(query: string, userId: number, ingredientIds: number[]): Observable<RecipeSummaryDto[]> {
    const params = new HttpParams()
      .set('query', query)
      .set('userId', userId.toString())
      .set('ingredientIds', ingredientIds.join(','));
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/search`, { params });
  }

  /** Combined semantic search + tag filter (AC11). */
  searchWithTags(query: string, userId: number, tagIds: number[]): Observable<RecipeSummaryDto[]> {
    const params = new HttpParams()
      .set('query', query)
      .set('userId', userId.toString())
      .set('tagIds', tagIds.join(','));
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/search`, { params });
  }

  /** List all reviews for a recipe, ordered by createdAt descending. */
  getReviews(recipeId: number): Observable<ReviewDto[]> {
    return this.http.get<ReviewDto[]>(`${this.baseUrl}/recipes/${recipeId}/reviews`);
  }

  /** Create a new review for a recipe (always POSTs — backend stores multiple per user per date). */
  createReview(recipeId: number, dto: CreateReviewPayload): Observable<ReviewDto> {
    return this.http.post<ReviewDto>(`${this.baseUrl}/recipes/${recipeId}/reviews`, dto);
  }

  /** Combined semantic search + ingredient filter + tag filter (AC11). */
  searchWithFilters(
    query: string,
    userId: number,
    ingredientIds: number[],
    tagIds: number[]
  ): Observable<RecipeSummaryDto[]> {
    let params = new HttpParams()
      .set('query', query)
      .set('userId', userId.toString());
    if (ingredientIds.length > 0) params = params.set('ingredientIds', ingredientIds.join(','));
    if (tagIds.length > 0) params = params.set('tagIds', tagIds.join(','));
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/search`, { params });
  }

  /** Upload an image for a recipe (multipart). Returns the updated recipe with new imageUrl. */
  uploadRecipeImage(recipeId: number, file: File): Observable<RecipeDetailDto> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<RecipeDetailDto>(`${this.baseUrl}/recipes/${recipeId}/image`, formData);
  }

  /** Generate an AI image for a recipe. Returns the updated recipe with new imageUrl. */
  generateRecipeImage(recipeId: number, payload: GenerateImagePayload): Observable<RecipeDetailDto> {
    return this.http.post<RecipeDetailDto>(`${this.baseUrl}/recipes/${recipeId}/image/generate`, payload);
  }

  /** Idealise an existing recipe image. Returns the updated recipe with new imageUrl. */
  idealiseRecipeImage(recipeId: number, payload: IdealiseImagePayload): Observable<RecipeDetailDto> {
    return this.http.post<RecipeDetailDto>(`${this.baseUrl}/recipes/${recipeId}/image/idealise`, payload);
  }
}
