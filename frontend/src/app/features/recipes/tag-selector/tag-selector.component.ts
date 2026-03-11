import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { TagOptionDto } from '../../../core/models/recipe.models';

interface TagGroup {
  categoryName: string;
  tags: TagOptionDto[];
}

@Component({
  selector: 'app-tag-selector',
  standalone: true,
  imports: [],
  templateUrl: './tag-selector.component.html',
  styleUrl: './tag-selector.component.scss'
})
export class TagSelectorComponent implements OnChanges {
  @Input() tags: TagOptionDto[] = [];
  @Input() selectedIds: Set<number> = new Set();
  @Output() selectionChange = new EventEmitter<number[]>();

  tagGroups: TagGroup[] = [];

  ngOnChanges(): void {
    this.buildGroups();
  }

  private buildGroups(): void {
    const map = new Map<string, TagOptionDto[]>();
    for (const tag of this.tags) {
      if (!map.has(tag.categoryName)) map.set(tag.categoryName, []);
      map.get(tag.categoryName)!.push(tag);
    }
    this.tagGroups = Array.from(map.entries()).map(([categoryName, tags]) => ({
      categoryName,
      tags
    }));
  }

  isSelected(id: number): boolean {
    return this.selectedIds.has(id);
  }

  toggle(id: number): void {
    const next = new Set(this.selectedIds);
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.selectionChange.emit(Array.from(next));
  }
}
