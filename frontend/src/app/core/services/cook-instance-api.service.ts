import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CookInstanceDetailDto,
  PatchCookIngredientPayload,
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

  completeCook(cookInstanceId: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/cook-instances/${cookInstanceId}/complete`, {});
  }
}
