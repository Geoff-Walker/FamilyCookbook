import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { Subject, filter, takeUntil } from 'rxjs';
import { UserToggleComponent } from './user-toggle/user-toggle.component';
import { HeaderStateService } from '../services/header-state.service';

type HeaderMode = 'list' | 'detail' | 'form';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, UserToggleComponent],
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.scss'
})
export class AppHeaderComponent implements OnInit, OnDestroy {
  mode: HeaderMode = 'list';
  pageTitle: string | null = null;

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly router: Router,
    private readonly headerState: HeaderStateService
  ) {}

  ngOnInit(): void {
    this.updateMode(this.router.url);

    this.router.events
      .pipe(
        filter(e => e instanceof NavigationEnd),
        takeUntil(this.destroy$)
      )
      .subscribe((e) => {
        this.updateMode((e as NavigationEnd).urlAfterRedirects);
      });

    this.headerState.pageTitle$
      .pipe(takeUntil(this.destroy$))
      .subscribe(title => {
        this.pageTitle = title;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  get isListMode(): boolean {
    return this.mode === 'list';
  }

  get isDetailMode(): boolean {
    return this.mode === 'detail';
  }

  private updateMode(url: string): void {
    const path = url.split('?')[0];
    if (path === '/' || path === '') {
      this.mode = 'list';
    } else if (/^\/recipes\/\d+$/.test(path)) {
      this.mode = 'detail';
    } else {
      this.mode = 'form';
    }
  }
}
