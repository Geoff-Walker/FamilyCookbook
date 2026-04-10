import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { CookInstanceApiService } from '../../../core/services/cook-instance-api.service';
import { CookInstanceDetailDto } from '../../../core/models/cook-instance.models';
import {
  IngredientChecklistComponent,
  IngredientPatchEvent
} from '../ingredient-checklist/ingredient-checklist.component';
import { HeaderStateService } from '../../../core/services/header-state.service';

type ViewState = 'loading' | 'loaded' | 'notFound' | 'error';

@Component({
  selector: 'app-cook-instance-page',
  standalone: true,
  imports: [CommonModule, RouterLink, IngredientChecklistComponent],
  templateUrl: './cook-instance-page.component.html',
  styleUrl: './cook-instance-page.component.scss'
})
export class CookInstancePageComponent implements OnInit {
  viewState: ViewState = 'loading';
  cookInstance: CookInstanceDetailDto | null = null;
  cookInstanceId!: number;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly cookApi: CookInstanceApiService,
    private readonly headerState: HeaderStateService
  ) {}

  ngOnInit(): void {
    this.cookInstanceId = Number(this.route.snapshot.paramMap.get('id'));
    this.load();
  }

  load(): void {
    this.viewState = 'loading';
    this.cookApi.getCookInstance(this.cookInstanceId).subscribe({
      next: (data) => {
        this.cookInstance = data;
        this.viewState = 'loaded';
        this.headerState.setPageTitle(data.recipeTitle);
      },
      error: (err: HttpErrorResponse) => {
        this.viewState = err.status === 404 ? 'notFound' : 'error';
        this.headerState.setPageTitle(null);
      }
    });
  }

  onIngredientPatched(event: IngredientPatchEvent): void {
    if (!this.cookInstance) return;
    this.cookApi.patchIngredient(this.cookInstanceId, event.ingredientId, event.patch).subscribe({
      error: () => {
        // Non-fatal — the UI state is already updated optimistically.
        // A future ticket can add error recovery.
      }
    });
  }

  onCompleteCook(): void {
    // Out of scope for this ticket — review modal is next ticket.
    // For now, POST complete and navigate back to recipe.
    if (!this.cookInstance) return;
    this.cookApi.completeCook(this.cookInstanceId).subscribe({
      next: () => {
        this.router.navigate(['/recipes', this.cookInstance!.recipeId]);
      },
      error: () => {
        // Handled in next ticket with review modal
      }
    });
  }

  formatStartedAt(iso: string): string {
    return new Date(iso).toLocaleTimeString('en-GB', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    });
  }
}
