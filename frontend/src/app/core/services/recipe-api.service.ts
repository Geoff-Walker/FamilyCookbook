import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class RecipeApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private readonly http: HttpClient) {}

  getRecipes(): Observable<any> {
    return this.http.get(`${this.baseUrl}/recipes`);
  }

  getRecipe(id: number): Observable<any> {
    return this.http.get(`${this.baseUrl}/recipes/${id}`);
  }

  createRecipe(dto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/recipes`, dto);
  }

  updateRecipe(id: number, dto: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/recipes/${id}`, dto);
  }

  deleteRecipe(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/recipes/${id}`);
  }
}
