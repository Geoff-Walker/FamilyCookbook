import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-recipe-list',
  standalone: true,
  imports: [CommonModule],
  template: `<p>recipe-list works</p>`,
  styleUrl: './recipe-list.component.scss'
})
export class RecipeListComponent {}
