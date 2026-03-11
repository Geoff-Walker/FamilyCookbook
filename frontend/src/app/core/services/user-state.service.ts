import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { UserDto } from '../models/recipe.models';

const STORAGE_KEY = 'walkerfcb-active-user';

@Injectable({
  providedIn: 'root'
})
export class UserStateService {
  private readonly usersSubject = new BehaviorSubject<UserDto[]>([]);
  private readonly activeUserIdSubject = new BehaviorSubject<number>(0);
  private readonly activeUserNameSubject = new BehaviorSubject<string>('');

  readonly users$: Observable<UserDto[]> = this.usersSubject.asObservable();
  readonly activeUserId$: Observable<number> = this.activeUserIdSubject.asObservable();
  readonly activeUserName$: Observable<string> = this.activeUserNameSubject.asObservable();

  get users(): UserDto[] {
    return this.usersSubject.getValue();
  }

  get activeUserId(): number {
    return this.activeUserIdSubject.getValue();
  }

  get activeUserName(): string {
    return this.activeUserNameSubject.getValue();
  }

  setUsers(users: UserDto[]): void {
    this.usersSubject.next(users);
  }

  setActiveUser(id: number): void {
    const user = this.usersSubject.getValue().find(u => u.id === id);
    if (!user) return;

    this.activeUserIdSubject.next(id);
    this.activeUserNameSubject.next(user.name);
    localStorage.setItem(STORAGE_KEY, user.themeName);
    document.documentElement.setAttribute('data-theme', user.themeName);
  }

  initFromStorage(users: UserDto[]): void {
    this.usersSubject.next(users);

    const storedTheme = localStorage.getItem(STORAGE_KEY);
    const matched = storedTheme
      ? users.find(u => u.themeName === storedTheme)
      : null;

    // Default to Helen if no stored preference or no match
    const defaultThemeName = 'helen';
    const target = matched ?? users.find(u => u.themeName === defaultThemeName) ?? users[0];

    if (!target) return;

    this.activeUserIdSubject.next(target.id);
    this.activeUserNameSubject.next(target.name);
    localStorage.setItem(STORAGE_KEY, target.themeName);
    document.documentElement.setAttribute('data-theme', target.themeName);
  }
}
