import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Integer stepper component for the batch multiplier field in the Add to Planner dialog.
 * Enforces min value (default 1), coerces all values to positive integers.
 * Emits (valueChange) on each change.
 */
@Component({
  selector: 'app-batch-stepper',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './batch-stepper.component.html',
  styleUrl: './batch-stepper.component.scss'
})
export class BatchStepperComponent implements OnChanges {
  /** Current value — must be a positive integer. */
  @Input() value = 1;

  /** Minimum allowed value. Defaults to 1. */
  @Input() min = 1;

  /** Emits the new integer value after each increment or decrement. */
  @Output() valueChange = new EventEmitter<number>();

  /** Internal integer-coerced display value. */
  displayValue = 1;

  ngOnChanges(): void {
    this.displayValue = this.coerce(this.value);
  }

  get isAtMin(): boolean {
    return this.displayValue <= this.min;
  }

  decrement(): void {
    if (this.isAtMin) return;
    this.displayValue = this.coerce(this.displayValue - 1);
    this.valueChange.emit(this.displayValue);
  }

  increment(): void {
    this.displayValue = this.coerce(this.displayValue + 1);
    this.valueChange.emit(this.displayValue);
  }

  /** Coerce any number to a positive integer at or above min. */
  private coerce(n: number): number {
    const clamped = Math.max(this.min, Math.floor(n));
    return isFinite(clamped) ? clamped : this.min;
  }
}
