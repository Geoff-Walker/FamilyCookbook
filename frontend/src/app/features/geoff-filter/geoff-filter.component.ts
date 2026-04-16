import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { GeoffFilterApiService } from '../../core/services/geoff-filter-api.service';
import { UserStateService } from '../../core/services/user-state.service';
import { RecipeSuggestionDto } from '../../core/models/geoff-filter.models';
import {
  SuggestionCardComponent,
  AcceptEvent,
  BacklogEvent,
  DeleteEvent
} from './suggestion-card/suggestion-card.component';

type TabId = 'queue' | 'backlog';

@Component({
  selector: 'app-geoff-filter',
  standalone: true,
  imports: [CommonModule, SuggestionCardComponent],
  templateUrl: './geoff-filter.component.html',
  styleUrl: './geoff-filter.component.scss'
})
export class GeoffFilterComponent implements OnInit, OnDestroy {

  activeTab: TabId = 'queue';
  activeUserId = 0;
  activeUserName = '';

  queueSuggestions: RecipeSuggestionDto[] = [];
  backlogSuggestions: RecipeSuggestionDto[] = [];

  isLoading = false;
  loadError: string | null = null;
  backlogLoaded = false;

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly api: GeoffFilterApiService,
    private readonly userState: UserStateService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.activeUserId = this.userState.activeUserId;
    this.activeUserName = this.userState.activeUserName;

    // Keep user ID in sync if they switch user while on this page
    this.userState.activeUserId$
      .pipe(takeUntil(this.destroy$))
      .subscribe(id => { this.activeUserId = id; });

    this.loadQueue();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ---------------------------------------------------------------------------
  // Derived state
  // ---------------------------------------------------------------------------

  get isGeoff(): boolean {
    return this.activeUserId === 1;
  }

  get queueCount(): number {
    return this.queueSuggestions.length;
  }

  get currentSuggestions(): RecipeSuggestionDto[] {
    return this.activeTab === 'queue' ? this.queueSuggestions : this.backlogSuggestions;
  }

  // ---------------------------------------------------------------------------
  // Tabs
  // ---------------------------------------------------------------------------

  setTab(tab: TabId): void {
    this.activeTab = tab;
    if (tab === 'backlog' && !this.backlogLoaded) {
      this.loadBacklog();
    }
  }

  // ---------------------------------------------------------------------------
  // Data loading
  // ---------------------------------------------------------------------------

  loadQueue(): void {
    this.isLoading = true;
    this.loadError = null;
    this.api.getSuggestions('pending')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          this.queueSuggestions = items;
          this.isLoading = false;
        },
        error: () => {
          this.loadError = 'Failed to load suggestions. Please try again.';
          this.isLoading = false;
        }
      });
  }

  loadBacklog(): void {
    this.isLoading = true;
    this.loadError = null;
    this.api.getSuggestions('backlogged')
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          this.backlogSuggestions = items;
          this.backlogLoaded = true;
          this.isLoading = false;
        },
        error: () => {
          this.loadError = 'Failed to load backlog. Please try again.';
          this.isLoading = false;
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Card actions
  // ---------------------------------------------------------------------------

  onAccept(event: AcceptEvent): void {
    this.api.acceptSuggestion(event.suggestionId, { requestingUserId: 1 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          // Update the card in-place to accepted state, then navigate
          const card = this.queueSuggestions.find(s => s.id === event.suggestionId);
          if (card) {
            card.status = 'accepted';
            card.recipeId = response.recipeId;
            card.recipeName = response.recipeName;
          }
          this.router.navigate(['/recipes', response.recipeId, 'edit']);
        },
        error: (err) => {
          if (err?.status === 403) {
            this.setCardAcceptError(event.suggestionId, 'Permission denied.');
          } else {
            this.setCardAcceptError(event.suggestionId, 'Failed to create recipe. Please try again.');
          }
        }
      });
  }

  onBacklog(event: BacklogEvent): void {
    this.api.backlogSuggestion(event.suggestionId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.queueSuggestions = this.queueSuggestions.filter(s => s.id !== event.suggestionId);
          // Invalidate backlog cache so it reloads on next visit
          this.backlogLoaded = false;
        },
        error: () => {
          // Silently ignore — card remains in queue
        }
      });
  }

  onDelete(event: DeleteEvent): void {
    this.api.deleteSuggestion(event.suggestionId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          if (this.activeTab === 'queue') {
            this.queueSuggestions = this.queueSuggestions.filter(s => s.id !== event.suggestionId);
          } else {
            this.backlogSuggestions = this.backlogSuggestions.filter(s => s.id !== event.suggestionId);
          }
        },
        error: () => {
          // Silently ignore — animation has already played, restore would be jarring
        }
      });
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  private acceptErrors = new Map<number, string>();

  getCardAcceptError(id: number): string | null {
    return this.acceptErrors.get(id) ?? null;
  }

  private setCardAcceptError(id: number, message: string): void {
    this.acceptErrors.set(id, message);
  }

  trackById(_: number, item: RecipeSuggestionDto): number {
    return item.id;
  }
}
