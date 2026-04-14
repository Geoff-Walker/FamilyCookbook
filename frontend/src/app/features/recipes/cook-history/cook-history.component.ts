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
  isExpanded = false;
  isLoading = true;
  loadError = false;

  constructor(
    private readonly cookApi: CookInstanceApiService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.cookApi.getCookHistory(this.recipeId).subscribe({
      next: (response) => {
        this.cookHistory = response.cookInstances;
        this.originalRecipeDate = response.originalRecipeDate;
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

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
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
