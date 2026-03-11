import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RecipeSummaryDto, RecipeDetailDto } from '../models/recipe.models';

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

  createRecipe(dto: any): Observable<RecipeDetailDto> {
    return this.http.post<RecipeDetailDto>(`${this.baseUrl}/recipes`, dto);
  }

  updateRecipe(id: number, dto: any): Observable<RecipeDetailDto> {
    return this.http.put<RecipeDetailDto>(`${this.baseUrl}/recipes/${id}`, dto);
  }

  deleteRecipe(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/recipes/${id}`);
  }
}
