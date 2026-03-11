import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-recipe-hero',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './recipe-hero.component.html',
  styleUrl: './recipe-hero.component.scss'
})
export class RecipeHeroComponent {
  @Input({ required: true }) title!: string;
  @Output() back = new EventEmitter<void>();
  @Output() edit = new EventEmitter<void>();
}
