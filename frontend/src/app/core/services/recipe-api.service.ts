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
  UserDto
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

  getUnits(): Observable<UnitOptionDto[]> {
    return this.http.get<UnitOptionDto[]>(`${this.baseUrl}/units`);
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

  /** AC5/AC8: Filter recipes by ingredient IDs — returns recipes containing ALL specified ingredients. */
  filterRecipes(ingredientIds: number[]): Observable<RecipeSummaryDto[]> {
    const params = new HttpParams().set('ingredientIds', ingredientIds.join(','));
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/recipes/filter`, { params });
  }

  /** AC10: Combined semantic search + ingredient filter. */
  searchWithIngredients(query: string, userId: number, ingredientIds: number[]): Observable<RecipeSummaryDto[]> {
    const params = new HttpParams()
      .set('query', query)
      .set('userId', userId.toString())
      .set('ingredientIds', ingredientIds.join(','));
    return this.http.get<RecipeSummaryDto[]>(`${this.baseUrl}/search`, { params });
  }
}
