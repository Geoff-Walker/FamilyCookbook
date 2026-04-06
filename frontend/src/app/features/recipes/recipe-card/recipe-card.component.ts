import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { RecipeSummaryDto } from '../../../core/models/recipe.models';

@Component({
  selector: 'app-recipe-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './recipe-card.component.html',
  styleUrl: './recipe-card.component.scss'
})
export class RecipeCardComponent {
  @Input({ required: true }) recipe!: RecipeSummaryDto;
  @Output() deleted = new EventEmitter<number>();

  showDeleteConfirm = false;

  constructor(private readonly router: Router) {}

  get totalTimeMinutes(): number | null {
    const prep = this.recipe.prepTimeMinutes ?? 0;
    const cook = this.recipe.cookTimeMinutes ?? 0;
    const total = prep + cook;
    return total > 0 ? total : null;
  }

  get visibleTags() {
    return this.recipe.tags.slice(0, 3);
  }

  get overflowTagCount(): number {
    return Math.max(0, this.recipe.tags.length - 3);
  }

  getRatingForUser(userId: number): number | null {
    const r = this.recipe.ratings.find(r => r.userId === userId);
    return r ? r.averageRating : null;
  }

  get helenRating(): number | null { return this.getRatingForUser(2); }
  get geoffRating(): number | null { return this.getRatingForUser(1); }

  formatRating(rating: number | null): string {
    if (rating === null) return '—';
    return rating.toFixed(1);
  }

  formatTime(minutes: number | null): string {
    if (!minutes) return '';
    if (minutes < 60) return `${minutes}m`;
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return m > 0 ? `${h}h ${m}m` : `${h}h`;
  }

  navigate(): void {
    this.router.navigate(['/recipes', this.recipe.id]);
  }

  promptDelete(event: Event): void {
    event.stopPropagation();
    this.showDeleteConfirm = true;
  }

  cancelDelete(event: Event): void {
    event.stopPropagation();
    this.showDeleteConfirm = false;
  }

  confirmDelete(event: Event): void {
    event.stopPropagation();
    this.deleted.emit(this.recipe.id);
    this.showDeleteConfirm = false;
  }
}
