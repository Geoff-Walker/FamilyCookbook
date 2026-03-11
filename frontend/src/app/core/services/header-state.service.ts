import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class HeaderStateService {
  private readonly pageTitleSubject = new BehaviorSubject<string | null>(null);

  readonly pageTitle$: Observable<string | null> = this.pageTitleSubject.asObservable();

  setPageTitle(title: string | null): void {
    this.pageTitleSubject.next(title);
  }
}
