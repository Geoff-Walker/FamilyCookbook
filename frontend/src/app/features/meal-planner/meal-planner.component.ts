import { Component } from '@angular/core';

@Component({
  selector: 'app-meal-planner',
  standalone: true,
  imports: [],
  template: `
    <div class="shell-page">
      <h1>Meal Planner</h1>
      <p>Coming soon — WAL-63.</p>
    </div>
  `,
  styles: [`
    .shell-page {
      padding: var(--space-8) var(--space-6);
    }
  `]
})
export class MealPlannerComponent {}
