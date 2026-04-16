import { Component, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CookInstanceApiService } from '../../../core/services/cook-instance-api.service';
import { CookInstanceSummaryDto } from '../../../core/models/cook-instance.models';

@Component({
  selector: 'app-cook-history',
  standalone: true,
  imports: [],
  templateUrl: './cook-history.component.html',
  styleUrl: './cook-history.component.scss'
})
export class CookHistoryComponent implements OnInit {
  @Input({ required: true }) recipeId!: number;

  cookHistory: CookInstanceSummaryDto[] = [];
  originalRecipeDate: string | null = null;
  hasOriginalSnapshot = false;
  isExpanded = false;
  isLoading = true;
  loadError = false;
  isRestoring = false;
  restoreError = false;

  constructor(
    private readonly cookApi: CookInstanceApiService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.cookApi.getCookHistory(this.recipeId).subscribe({
      next: (response) => {
        this.cookHistory = response.cookInstances;
        this.originalRecipeDate = response.originalRecipeDate;
        this.hasOriginalSnapshot = response.hasOriginalSnapshot;
        this.isLoading = false;
      },
      error: () => {
        this.loadError = true;
        this.isLoading = false;
      }
    });
  }

  toggleExpand(): void {
    this.isExpanded = !this.isExpanded;
  }

  onDelete(id: number): void {
    this.cookApi.deleteCookInstance(id).subscribe({
      next: () => {
        this.cookHistory = this.cookHistory.filter(row => row.id !== id);
      }
    });
  }

  navigateToCook(id: number): void {
    this.router.navigate(['/cook', id]);
  }

  onRestoreOriginal(): void {
    this.isRestoring = true;
    this.restoreError = false;
    this.cookApi.restoreOriginal(this.recipeId).subscribe({
      next: () => {
        // Full page reload — the ingredient list on the recipe detail page must reflect the restore
        window.location.reload();
      },
      error: () => {
        this.isRestoring = false;
        this.restoreError = true;
      }
    });
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  }

  formatDateShort(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short'
    });
  }

  starDisplay(rating: number): string {
    const rounded = Math.round(rating);
    return '★'.repeat(rounded) + '☆'.repeat(5 - rounded);
  }

  getRating(row: CookInstanceSummaryDto, userId: number): number | null {
    const review = row.reviews.find(r => r.userId === userId);
    return review != null ? review.rating : null;
  }
}
