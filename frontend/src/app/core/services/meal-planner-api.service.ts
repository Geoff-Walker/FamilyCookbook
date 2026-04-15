import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateMealPlanSlotPayload, MealPlanSlotDto } from '../models/meal-planner.models';

@Injectable({
  providedIn: 'root'
})
export class MealPlannerApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  /**
   * Load all meal plan slots within a date range (inclusive).
   * @param from ISO date string e.g. "2026-04-01"
   * @param to   ISO date string e.g. "2026-04-30"
   */
  getSlots(from: string, to: string): Observable<MealPlanSlotDto[]> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<MealPlanSlotDto[]>(`${this.baseUrl}/meal-plan-slots`, { params });
  }

  /** Create a new meal plan slot. */
  createSlot(payload: CreateMealPlanSlotPayload): Observable<MealPlanSlotDto> {
    return this.http.post<MealPlanSlotDto>(`${this.baseUrl}/meal-plan-slots`, payload);
  }

  /** Hard-delete a meal plan slot by id. */
  deleteSlot(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/meal-plan-slots/${id}`);
  }
}
