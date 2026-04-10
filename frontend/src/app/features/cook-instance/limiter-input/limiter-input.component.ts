import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CookInstanceIngredientDto } from '../../../core/models/cook-instance.models';

@Component({
  selector: 'app-limiter-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './limiter-input.component.html',
  styleUrl: './limiter-input.component.scss'
})
export class LimiterInputComponent {
  @Input() ingredient!: CookInstanceIngredientDto;
  @Input() baseAmount!: number;
  @Output() limiterQuantityChange = new EventEmitter<number | null>();

  limiterQty: number | null = null;

  onInput(value: string): void {
    const parsed = value === '' ? null : parseFloat(value);
    this.limiterQty = parsed;
    this.limiterQuantityChange.emit(parsed);
  }
}
