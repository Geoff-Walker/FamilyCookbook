import {
  Component,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  BehaviorSubject,
  combineLatest,
  Subject,
  debounceTime,
  distinctUntilChanged,
  filter,
  switchMap,
  takeUntil,
  of
} from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { UserStateService } from '../../../core/services/user-state.service';
import { RecipeSummaryDto } from '../../../core/models/recipe.models';

export type SearchState = 'idle' | 'loading' | 'results' | 'empty' | 'error';

export interface SearchResult {
  state: SearchState;
  recipes: RecipeSummaryDto[];
  query: string;
}

@Component({
  selector: 'app-semantic-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './semantic-search.component.html',
  styleUrl: './semantic-search.component.scss'
})
export class SemanticSearchComponent implements OnInit, OnDestroy {
  /** Emits whenever the search state changes (results, idle, error, etc.) */
  @Output() searchStateChange = new EventEmitter<SearchResult>();

  query = '';
  isLoading = false;
  isFocused = false;

  private readonly querySubject = new BehaviorSubject<string>('');
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly recipeApi: RecipeApiService,
    private readonly userState: UserStateService
  ) {}

  ngOnInit(): void {
    // Combine the debounced query stream with the active user ID.
    // Re-runs the search when either the query or the active user changes.
    const query$ = this.querySubject.pipe(
      debounceTime(400),
      distinctUntilChanged()
    );

    combineLatest([query$, this.userState.activeUserId$])
      .pipe(
        takeUntil(this.destroy$),
        map(([q, userId]) => ({ q, userId }))
      )
      .subscribe(({ q, userId }) => {
        if (q.length < 3) {
          // AC2 / AC8: query under minimum length — emit idle to restore full list
          this.isLoading = false;
          this.searchStateChange.emit({ state: 'idle', recipes: [], query: q });
          return;
        }

        if (userId === 0) {
          // User not yet initialised — skip
          return;
        }

        this.isLoading = true;
        this.searchStateChange.emit({ state: 'loading', recipes: [], query: q });

        this.recipeApi.search(q, userId)
          .pipe(
            takeUntil(this.destroy$),
            catchError(() => {
              this.isLoading = false;
              this.searchStateChange.emit({ state: 'error', recipes: [], query: q });
              return of(null);
            })
          )
          .subscribe(results => {
            if (results === null) return; // handled by catchError

            this.isLoading = false;
            if (results.length === 0) {
              this.searchStateChange.emit({ state: 'empty', recipes: [], query: q });
            } else {
              this.searchStateChange.emit({ state: 'results', recipes: results, query: q });
            }
          });
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onQueryChange(value: string): void {
    this.query = value;
    this.querySubject.next(value);
  }

  onClear(): void {
    this.query = '';
    this.querySubject.next('');
  }

  onFocus(): void {
    this.isFocused = true;
  }

  onBlur(): void {
    this.isFocused = false;
  }
}
