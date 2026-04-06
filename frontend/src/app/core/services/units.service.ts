import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { RecipeApiService } from './recipe-api.service';
import { UnitOptionDto } from '../models/recipe.models';

/**
 * Shared singleton service for units of measurement.
 *
 * Exposes `units$` as an observable list of all units.
 * Call `refresh()` after creating a new unit to push the updated
 * list to all subscribers (e.g. every ingredient-editor row).
 *
 * WAL-56: initial implementation. WAL-59 (admin route) will also consume this.
 */
@Injectable({ providedIn: 'root' })
export class UnitsService {
  private readonly api = inject(RecipeApiService);

  private readonly _units$ = new BehaviorSubject<UnitOptionDto[]>([]);

  /** Observable list of all units. Subscribers automatically receive updates after refresh(). */
  readonly units$: Observable<UnitOptionDto[]> = this._units$.asObservable();

  /** Returns the current snapshot — useful for one-off reads without subscribing. */
  get snapshot(): UnitOptionDto[] {
    return this._units$.getValue();
  }

  /**
   * Fetch units from the API and push to all subscribers.
   * Call once on app init (or recipe form init) and after creating a new unit.
   */
  refresh(): void {
    this.api.getUnits().subscribe(units => this._units$.next(units));
  }

  /**
   * Create a new unit via POST /api/units, then refresh the units list.
   * Returns an Observable that emits the created UnitOptionDto on success,
   * or errors with the HTTP response on failure (caller handles 409).
   */
  createUnit(name: string, abbreviation?: string): Observable<UnitOptionDto> {
    return new Observable(observer => {
      this.api.createUnit(name, abbreviation).subscribe({
        next: unit => {
          this.refresh();
          observer.next(unit);
          observer.complete();
        },
        error: err => observer.error(err)
      });
    });
  }
}
