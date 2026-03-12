import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject, firstValueFrom, takeUntil } from 'rxjs';

import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { RecipeDetailDto, SaveRecipePayload, TagOptionDto, UnitOptionDto } from '../../../core/models/recipe.models';
import { StageEditorComponent } from '../stage-editor/stage-editor.component';
import { TagSelectorComponent } from '../tag-selector/tag-selector.component';

type FormMode = 'add' | 'edit';
type PageState = 'loading' | 'ready' | 'notFound' | 'error';
type SaveState = 'idle' | 'saving' | 'error';

@Component({
  selector: 'app-recipe-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, StageEditorComponent, TagSelectorComponent],
  templateUrl: './recipe-form.component.html',
  styleUrl: './recipe-form.component.scss'
})
export class RecipeFormComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(RecipeApiService);
  private readonly destroy$ = new Subject<void>();

  mode: FormMode = 'add';
  pageState: PageState = 'loading';
  saveState: SaveState = 'idle';
  recipeId: number | null = null;
  editTitle = '';

  allTags: TagOptionDto[] = [];
  units: UnitOptionDto[] = [];
  selectedTagIds = new Set<number>();
  successMessage = '';

  form!: FormGroup;

  get stagesArray(): FormArray {
    return this.form.get('stages') as FormArray;
  }

  stageGroup(index: number): FormGroup {
    return this.stagesArray.at(index) as FormGroup;
  }

  get stageIndices(): number[] {
    return Array.from({ length: this.stagesArray.length }, (_, i) => i);
  }

  get titleControl() {
    return this.form.get('title');
  }

  get imageUrlControl() {
    return this.form.get('imageUrl');
  }

  get formTitle(): string {
    if (this.mode === 'add') return 'New Recipe';
    return this.editTitle ? `Edit ${this.editTitle}` : 'Edit Recipe';
  }

  ngOnInit(): void {
    this.form = this.buildForm();
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.mode = 'edit';
      this.recipeId = +idParam;
      this.loadForEdit(this.recipeId);
    } else {
      this.mode = 'add';
      this.loadReferenceData().then(() => {
        this.pageState = 'ready';
      });
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private buildForm(): FormGroup {
    return this.fb.group({
      title: ['', Validators.required],
      description: [''],
      source: [''],
      imageUrl: ['', [Validators.pattern(/^https?:.*/)]],
      prepTimeMinutes: [null as number | null],
      cookTimeMinutes: [null as number | null],
      servings: [null as number | null],
      stages: this.fb.array([this.createStageGroup()])
    });
  }

  private async loadReferenceData(): Promise<void> {
    const [tags, units] = await Promise.all([
      firstValueFrom(this.api.getTags()),
      firstValueFrom(this.api.getUnits())
    ]);
    this.allTags = tags;
    this.units = units;
  }

  private loadForEdit(id: number): void {
    Promise.all([
      firstValueFrom(this.api.getRecipe(id)),
      firstValueFrom(this.api.getTags()),
      firstValueFrom(this.api.getUnits())
    ]).then(([recipe, tags, units]) => {
      this.allTags = tags;
      this.units = units;
      this.populateForm(recipe);
      this.pageState = 'ready';
    }).catch((err: HttpErrorResponse) => {
      this.pageState = err?.status === 404 ? 'notFound' : 'error';
    });
  }

  private populateForm(recipe: RecipeDetailDto): void {
    this.editTitle = recipe.title;
    this.form.patchValue({
      title: recipe.title,
      description: recipe.description ?? '',
      source: recipe.source ?? '',
      imageUrl: recipe.imageUrl ?? '',
      prepTimeMinutes: recipe.prepTimeMinutes,
      cookTimeMinutes: recipe.cookTimeMinutes,
      servings: recipe.servings
    });

    this.selectedTagIds = new Set(recipe.tags.map(t => t.id));

    const stagesArray = this.stagesArray;
    stagesArray.clear();
    for (const stage of recipe.stages) {
      const sg = this.createStageGroup(stage.name, stage.description ?? '');
      const stepsArray = sg.get('steps') as FormArray;
      stepsArray.clear();
      for (const step of stage.steps) {
        stepsArray.push(this.createStepGroup(step.instruction));
      }
      if (stepsArray.length === 0) {
        stepsArray.push(this.createStepGroup());
      }
      const ingredientsArray = sg.get('ingredients') as FormArray;
      for (const ing of stage.ingredients) {
        ingredientsArray.push(this.createIngredientGroup(
          ing.ingredientName,
          ing.amount ?? '',
          ing.unitId,
          ing.notes ?? ''
        ));
      }
      stagesArray.push(sg);
    }
    if (stagesArray.length === 0) {
      stagesArray.push(this.createStageGroup());
    }
  }

  createStageGroup(name = '', description = ''): FormGroup {
    return this.fb.group({
      name: [name],
      description: [description],
      steps: this.fb.array([this.createStepGroup()]),
      ingredients: this.fb.array([])
    });
  }

  createStepGroup(instruction = ''): FormGroup {
    return this.fb.group({ instruction: [instruction] });
  }

  createIngredientGroup(
    ingredientName = '',
    amount = '',
    unitId: number | null = null,
    notes = ''
  ): FormGroup {
    return this.fb.group({
      ingredientName: [ingredientName],
      amount: [amount],
      unitId: [unitId ?? ''],
      notes: [notes]
    });
  }

  addStage(): void {
    this.stagesArray.push(this.createStageGroup());
  }

  removeStage(index: number): void {
    if (this.stagesArray.length > 1) {
      this.stagesArray.removeAt(index);
    }
  }

  onTagSelectionChange(ids: number[]): void {
    this.selectedTagIds = new Set(ids);
  }

  cancel(): void {
    if (this.mode === 'edit' && this.recipeId) {
      this.router.navigate(['/recipes', this.recipeId]);
    } else {
      this.router.navigate(['/']);
    }
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saveState = 'saving';
    this.successMessage = '';

    const payload = this.buildPayload();
    const call$ = this.mode === 'add'
      ? this.api.createRecipe(payload)
      : this.api.updateRecipe(this.recipeId!, payload);

    call$.pipe(takeUntil(this.destroy$)).subscribe({
      next: (recipe) => {
        this.saveState = 'idle';
        this.successMessage = this.mode === 'add' ? 'Recipe created!' : 'Recipe updated!';
        setTimeout(() => this.router.navigate(['/recipes', recipe.id]), 1200);
      },
      error: () => {
        this.saveState = 'error';
      }
    });
  }

  private buildPayload(): SaveRecipePayload {
    const raw = this.form.value;
    return {
      title: (raw.title ?? '').trim(),
      description: raw.description?.trim() || null,
      source: raw.source?.trim() || null,
      imageUrl: raw.imageUrl?.trim() || null,
      prepTimeMinutes: raw.prepTimeMinutes ? +raw.prepTimeMinutes : null,
      cookTimeMinutes: raw.cookTimeMinutes ? +raw.cookTimeMinutes : null,
      servings: raw.servings ? +raw.servings : null,
      tagIds: Array.from(this.selectedTagIds),
      stages: (raw.stages ?? []).map((s: any) => ({
        name: (s.name ?? '').trim(),
        description: s.description?.trim() || null,
        steps: (s.steps ?? [])
          .filter((st: any) => st.instruction?.trim())
          .map((st: any) => ({ instruction: st.instruction.trim() })),
        ingredients: (s.ingredients ?? [])
          .filter((ing: any) => ing.ingredientName?.trim())
          .map((ing: any) => ({
            ingredientName: ing.ingredientName.trim(),
            amount: ing.amount?.trim() || null,
            unitId: (ing.unitId !== null && ing.unitId !== '') ? +ing.unitId : null,
            notes: ing.notes?.trim() || null
          }))
      }))
    };
  }
}
