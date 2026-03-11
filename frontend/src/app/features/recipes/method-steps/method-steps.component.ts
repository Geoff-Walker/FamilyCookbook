import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RecipeDetailStepDto } from '../../../core/models/recipe.models';

type StepState = 'normal' | 'active' | 'complete';

@Component({
  selector: 'app-method-steps',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './method-steps.component.html',
  styleUrl: './method-steps.component.scss'
})
export class MethodStepsComponent {
  @Input({ required: true }) steps!: RecipeDetailStepDto[];

  /** Offset applied to step display numbers (for multi-stage continuity) */
  @Input() startNumber: number = 1;

  private readonly stepStates = new Map<number, StepState>();

  getState(id: number): StepState {
    return this.stepStates.get(id) ?? 'normal';
  }

  tap(id: number): void {
    const current = this.getState(id);
    const next: StepState = current === 'normal' ? 'active'
      : current === 'active' ? 'complete'
      : 'normal';
    this.stepStates.set(id, next);
  }

  displayNumber(index: number): number {
    return this.startNumber + index;
  }
}
