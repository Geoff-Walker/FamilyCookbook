import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppHeaderComponent } from './core/header/app-header.component';
import { RecipeApiService } from './core/services/recipe-api.service';
import { UserStateService } from './core/services/user-state.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, AppHeaderComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  constructor(
    private readonly recipeApi: RecipeApiService,
    private readonly userState: UserStateService
  ) {}

  ngOnInit(): void {
    this.recipeApi.getUsers().subscribe({
      next: (users) => {
        this.userState.initFromStorage(users);
      },
      error: () => {
        // API unavailable — apply a safe default theme so the UI is at least usable
        document.documentElement.setAttribute('data-theme', 'helen');
      }
    });
  }
}
