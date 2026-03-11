import {
  Component,
  EventEmitter,
  OnInit,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { TagFilterService, TagFilterState } from '../../../core/services/tag-filter.service';
import { TagOptionDto } from '../../../core/models/recipe.models';

export { TagFilterState };

/** Tags grouped under a single category name for rendering (AC8). */
export interface TagGroup {
  categoryName: string;
  tags: TagOptionDto[];
}

@Component({
  selector: 'app-tag-filter',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tag-filter.component.html',
  styleUrl: './tag-filter.component.scss'
})
export class TagFilterComponent implements OnInit {
  /** Emits whenever the selected tag set changes (toggle, clear). */
  @Output() tagFilterStateChange = new EventEmitter<TagFilterState>();

  // -------------------------------------------------------------------------
  // Panel open/close
  // -------------------------------------------------------------------------

  isOpen = false;

  // -------------------------------------------------------------------------
  // Tags state
  // -------------------------------------------------------------------------

  /** Tags grouped by category for rendering (AC8). */
  tagGroups: TagGroup[] = [];

  /** True while the initial tags GET is in flight (AC1). */
  isLoadingTags = false;

  /** Shown when the initial GET /api/tags fails (AC1). */
  tagsLoadError = false;

  // -------------------------------------------------------------------------

  constructor(
    private readonly recipeApi: RecipeApiService,
    private readonly tagFilterService: TagFilterService
  ) {}

  ngOnInit(): void {
    this.loadTags();
  }

  // -------------------------------------------------------------------------
  // Tag loading (AC1)
  // -------------------------------------------------------------------------

  private loadTags(): void {
    this.isLoadingTags = true;
    this.tagsLoadError = false;

    this.recipeApi.getTags().subscribe({
      next: (tags) => {
        this.isLoadingTags = false;
        this.tagGroups = this.groupByCategory(tags);
      },
      error: () => {
        this.isLoadingTags = false;
        this.tagsLoadError = true;
      }
    });
  }

  private groupByCategory(tags: TagOptionDto[]): TagGroup[] {
    const map = new Map<string, TagOptionDto[]>();
    for (const tag of tags) {
      const existing = map.get(tag.categoryName);
      if (existing) {
        existing.push(tag);
      } else {
        map.set(tag.categoryName, [tag]);
      }
    }
    // Map preserves insertion order; the API returns tags sorted by category then name
    return Array.from(map.entries()).map(([categoryName, tagList]) => ({
      categoryName,
      tags: tagList
    }));
  }

  // -------------------------------------------------------------------------
  // Panel toggle
  // -------------------------------------------------------------------------

  togglePanel(): void {
    this.isOpen = !this.isOpen;
  }

  // -------------------------------------------------------------------------
  // Chip toggle (AC2, AC3)
  // -------------------------------------------------------------------------

  toggleTag(id: number): void {
    this.tagFilterService.toggleTag(id);
    this.emitState();
  }

  isTagSelected(id: number): boolean {
    return this.tagFilterService.isSelected(id);
  }

  // -------------------------------------------------------------------------
  // Clear all (AC6, AC-D8)
  // -------------------------------------------------------------------------

  clearAll(): void {
    this.tagFilterService.clearAll();
    this.emitState();
  }

  // -------------------------------------------------------------------------
  // Helpers
  // -------------------------------------------------------------------------

  get selectedCount(): number {
    return this.tagFilterService.selectedCount;
  }

  private emitState(): void {
    this.tagFilterStateChange.emit(this.tagFilterService.buildState());
  }
}
