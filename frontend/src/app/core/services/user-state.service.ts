import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserStateService {
  private readonly activeUserIdSubject = new BehaviorSubject<number>(1);

  readonly activeUserId$: Observable<number> = this.activeUserIdSubject.asObservable();

  setActiveUser(id: number): void {
    this.activeUserIdSubject.next(id);
  }
}
