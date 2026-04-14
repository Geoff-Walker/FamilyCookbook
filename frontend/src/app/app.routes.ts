import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/recipes/recipe-list/recipe-list.component').then(
        m => m.RecipeListComponent
      )
  },
  {
    path: 'recipes/new',
    loadComponent: () =>
      import('./features/recipes/recipe-form/recipe-form.component').then(
        m => m.RecipeFormComponent
      )
  },
  {
    path: 'recipes/:id',
    loadComponent: () =>
      import('./features/recipes/recipe-detail/recipe-detail.component').then(
        m => m.RecipeDetailComponent
      )
  },
  {
    path: 'recipes/:id/edit',
    loadComponent: () =>
      import('./features/recipes/recipe-form/recipe-form.component').then(
        m => m.RecipeFormComponent
      )
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./features/admin/admin.component').then(
        m => m.AdminComponent
      )
  },
  {
    path: 'meal-planner',
    loadComponent: () =>
      import('./features/meal-planner/meal-planner.component').then(
        m => m.MealPlannerComponent
      )
  },
  {
    path: 'geoff-filter',
    loadComponent: () =>
      import('./features/geoff-filter/geoff-filter.component').then(
        m => m.GeoffFilterComponent
      )
  },
  {
    // Completed cook view — full ingredient comparison, promote, add review (WAL-76)
    path: 'cook/:id',
    loadComponent: () =>
      import('./features/cook-instance/cook-instance-view-page/cook-instance-view-page.component').then(
        m => m.CookInstanceViewPageComponent
      )
  },
  {
    // Active in-progress cook — checklist, complete-cook modal (WAL-72)
    path: 'cook/:id/active',
    loadComponent: () =>
      import('./features/cook-instance/cook-instance-page/cook-instance-page.component').then(
        m => m.CookInstancePageComponent
      )
  },
  {
    path: '**',
    redirectTo: ''
  }
];
