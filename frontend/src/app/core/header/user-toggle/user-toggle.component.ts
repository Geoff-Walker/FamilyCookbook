import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserStateService } from '../../services/user-state.service';
import { UserDto } from '../../models/recipe.models';

@Component({
  selector: 'app-user-toggle',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './user-toggle.component.html',
  styleUrl: './user-toggle.component.scss'
})
export class UserToggleComponent implements OnInit {
  users: UserDto[] = [];
  activeUserId = 0;

  constructor(private readonly userState: UserStateService) {}

  ngOnInit(): void {
    this.users = this.userState.users;
    this.activeUserId = this.userState.activeUserId;

    this.userState.activeUserId$.subscribe(id => {
      this.activeUserId = id;
    });

    this.userState.users$.subscribe(users => {
      this.users = users;
    });
  }

  selectUser(user: UserDto): void {
    if (user.id === this.activeUserId) return;
    this.userState.setActiveUser(user.id);
  }

  isActive(user: UserDto): boolean {
    return user.id === this.activeUserId;
  }
}
