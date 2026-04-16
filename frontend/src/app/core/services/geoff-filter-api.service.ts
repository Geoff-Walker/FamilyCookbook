import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AcceptSuggestionPayload,
  AcceptSuggestionResponse,
  CreateSuggestionPayload,
  RecipeSuggestionDto,
  SuggestionStatus
} from '../models/geoff-filter.models';

@Injectable({
  providedIn: 'root'
})
export class GeoffFilterApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  createSuggestion(payload: CreateSuggestionPayload): Observable<RecipeSuggestionDto> {
    return this.http.post<RecipeSuggestionDto>(`${this.baseUrl}/recipe-suggestions`, payload);
  }

  getSuggestions(status: SuggestionStatus): Observable<RecipeSuggestionDto[]> {
    const params = new HttpParams().set('status', status);
    return this.http.get<RecipeSuggestionDto[]>(`${this.baseUrl}/recipe-suggestions`, { params });
  }

  acceptSuggestion(id: number, payload: AcceptSuggestionPayload): Observable<AcceptSuggestionResponse> {
    return this.http.post<AcceptSuggestionResponse>(`${this.baseUrl}/recipe-suggestions/${id}/accept`, payload);
  }

  backlogSuggestion(id: number): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/recipe-suggestions/${id}/backlog`, {});
  }

  deleteSuggestion(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/recipe-suggestions/${id}`);
  }
}
