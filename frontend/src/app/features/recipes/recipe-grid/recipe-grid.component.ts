import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RecipeSummaryDto } from '../../../core/models/recipe.models';
import { RecipeCardComponent } from '../recipe-card/recipe-card.component';

@Component({
  selector: 'app-recipe-grid',
  standalone: true,
  imports: [CommonModule, RecipeCardComponent],
  templateUrl: './recipe-grid.component.html',
  styleUrl: './recipe-grid.component.scss'
})
export class RecipeGridComponent {
  @Input({ required: true }) recipes!: RecipeSummaryDto[];
  @Output() recipeDeleted = new EventEmitter<number>();
}
