import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CompleteCookPayload,
  CookHistoryResponseDto,
  CookInstanceDetailDto,
  PatchCookIngredientPayload,
  PromoteResultDto,
  RestoreResultDto,
  RecipeVersionSummaryDto,
  StartCookPayload
} from '../models/cook-instance.models';

@Injectable({
  providedIn: 'root'
})
export class CookInstanceApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  startCook(payload: StartCookPayload): Observable<CookInstanceDetailDto> {
    return this.http.post<CookInstanceDetailDto>(`${this.baseUrl}/cook-instances`, payload);
  }

  getCookInstance(id: number): Observable<CookInstanceDetailDto> {
    return this.http.get<CookInstanceDetailDto>(`${this.baseUrl}/cook-instances/${id}`);
  }

  patchIngredient(
    cookInstanceId: number,
    ingredientId: number,
    patch: PatchCookIngredientPayload
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.baseUrl}/cook-instances/${cookInstanceId}/ingredients/${ingredientId}`,
      patch
    );
  }

  completeCook(cookInstanceId: number, payload: CompleteCookPayload): Observable<CookInstanceDetailDto> {
    return this.http.post<CookInstanceDetailDto>(
      `${this.baseUrl}/cook-instances/${cookInstanceId}/complete`,
      payload
    );
  }

  getCookHistory(recipeId: number): Observable<CookHistoryResponseDto> {
    return this.http.get<CookHistoryResponseDto>(`${this.baseUrl}/recipes/${recipeId}/cook-instances`);
  }

  getVersionsByRecipe(recipeId: number): Observable<RecipeVersionSummaryDto[]> {
    return this.http.get<RecipeVersionSummaryDto[]>(`${this.baseUrl}/recipes/${recipeId}/versions`);
  }

  deleteCookInstance(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/cook-instances/${id}`);
  }

  removeCookIngredient(cookInstanceId: number, ingredientId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/cook-instances/${cookInstanceId}/ingredients/${ingredientId}`
    );
  }

  /**
   * Promote a completed cook instance's actuals to the main recipe.
   * The active user ID is passed via the X-User-Id header, required by the backend.
   */
  promoteCook(cookInstanceId: number, userId: number): Observable<PromoteResultDto> {
    return this.http.post<PromoteResultDto>(
      `${this.baseUrl}/cook-instances/${cookInstanceId}/promote`,
      {},
      { headers: { 'X-User-Id': userId.toString() } }
    );
  }

  /**
   * Restore the recipe's ingredient list from the original pre-promotion snapshot.
   * Only available when hasOriginalSnapshot = true on the cook history response.
   */
  restoreOriginal(recipeId: number): Observable<RestoreResultDto> {
    return this.http.post<RestoreResultDto>(
      `${this.baseUrl}/recipes/${recipeId}/restore-original`,
      {}
    );
  }
}
