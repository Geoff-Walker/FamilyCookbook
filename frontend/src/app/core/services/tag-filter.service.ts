import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface TagFilterState {
  /** True when at least one tag chip is selected */
  hasFilters: boolean;
  /** Currently selected tag IDs (non-empty when hasFilters is true) */
  tagIds: number[];
}

/**
 * Singleton service that holds selected tag filter state across route navigation (AC10).
 * TagFilterComponent reads from and writes to this service; RecipeListComponent
 * subscribes to changes via the emitted TagFilterState.
 */
@Injectable({
  providedIn: 'root'
})
export class TagFilterService {
  private readonly _selectedTagIds = new Set<number>();

  /** Returns a copy of the current selected tag IDs (sorted for stability). */
  getSelectedTagIds(): number[] {
    return Array.from(this._selectedTagIds).sort((a, b) => a - b);
  }

  toggleTag(id: number): void {
    if (this._selectedTagIds.has(id)) {
      this._selectedTagIds.delete(id);
    } else {
      this._selectedTagIds.add(id);
    }
  }

  isSelected(id: number): boolean {
    return this._selectedTagIds.has(id);
  }

  clearAll(): void {
    this._selectedTagIds.clear();
  }

  get selectedCount(): number {
    return this._selectedTagIds.size;
  }

  buildState(): TagFilterState {
    const ids = this.getSelectedTagIds();
    return { hasFilters: ids.length > 0, tagIds: ids };
  }
}
