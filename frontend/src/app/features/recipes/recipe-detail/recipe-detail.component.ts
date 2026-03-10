import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-recipe-detail',
  standalone: true,
  imports: [CommonModule],
  template: `<p>recipe-detail works</p>`,
  styleUrl: './recipe-detail.component.scss'
})
export class RecipeDetailComponent {}
