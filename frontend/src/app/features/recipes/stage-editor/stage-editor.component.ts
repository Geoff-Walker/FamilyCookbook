import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { ReactiveFormsModule, FormArray, FormBuilder, FormGroup } from '@angular/forms';
import { UnitOptionDto } from '../../../core/models/recipe.models';
import { IngredientEditorComponent } from '../ingredient-editor/ingredient-editor.component';

@Component({
  selector: 'app-stage-editor',
  standalone: true,
  imports: [ReactiveFormsModule, IngredientEditorComponent],
  templateUrl: './stage-editor.component.html',
  styleUrl: './stage-editor.component.scss'
})
export class StageEditorComponent {
  private readonly fb = inject(FormBuilder);

  @Input({ required: true }) stageGroup!: FormGroup;
  @Input() stageIndex = 0;
  @Input() canRemove = false;
  @Input() units: UnitOptionDto[] = [];
  @Output() removed = new EventEmitter<void>();

  get stepsArray(): FormArray {
    return this.stageGroup.get('steps') as FormArray;
  }

  get ingredientsArray(): FormArray {
    return this.stageGroup.get('ingredients') as FormArray;
  }

  get stepIndices(): number[] {
    return Array.from({ length: this.stepsArray.length }, (_, i) => i);
  }

  get ingredientIndices(): number[] {
    return Array.from({ length: this.ingredientsArray.length }, (_, i) => i);
  }

  stepGroup(index: number): FormGroup {
    return this.stepsArray.at(index) as FormGroup;
  }

  ingredientGroup(index: number): FormGroup {
    return this.ingredientsArray.at(index) as FormGroup;
  }

  addStep(): void {
    this.stepsArray.push(this.fb.group({ instruction: [''] }));
  }

  removeStep(index: number): void {
    if (this.stepsArray.length > 1) {
      this.stepsArray.removeAt(index);
    }
  }

  addIngredient(): void {
    this.ingredientsArray.push(this.fb.group({
      ingredientId: [null],
      ingredientDisplayName: [''],
      amount: [null],
      unitId: [''],
      notes: ['']
    }));
  }

  removeIngredient(index: number): void {
    this.ingredientsArray.removeAt(index);
  }
}
