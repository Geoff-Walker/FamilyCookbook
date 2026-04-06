import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule, FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject, firstValueFrom, takeUntil } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSnackBarModule } from '@angular/material/snack-bar';

import { RecipeApiService } from '../../../core/services/recipe-api.service';
import { UnitsService } from '../../../core/services/units.service';
import { RecipeDetailDto, SaveRecipePayload, TagOptionDto, UnitOptionDto, ImageStyle } from '../../../core/models/recipe.models';
import { StageEditorComponent } from '../stage-editor/stage-editor.component';
import { TagSelectorComponent } from '../tag-selector/tag-selector.component';

type FormMode = 'add' | 'edit';
type PageState = 'loading' | 'ready' | 'notFound' | 'error';
type SaveState = 'idle' | 'saving' | 'error';
type ImageAction = 'upload' | 'generate' | 'idealise';

export interface ImageStyleOption {
  label: string;
  value: ImageStyle;
}

@Component({
  selector: 'app-recipe-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, StageEditorComponent, TagSelectorComponent, MatSnackBarModule],
  templateUrl: './recipe-form.component.html',
  styleUrl: './recipe-form.component.scss'
})
export class RecipeFormComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly api = inject(RecipeApiService);
  private readonly unitsService = inject(UnitsService);
  private readonly snackBar = inject(MatSnackBar);
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

  /** Current image URL — updated optimistically on each image action success. */
  currentImageUrl: string | null = null;

  /** Which image action (if any) is currently in progress. */
  imageActionInProgress: ImageAction | null = null;

  /** Selected style for Generate / Idealise. */
  selectedStyle: ImageStyle | '' = '';

  /** Free text detail for Generate / Idealise. */
  freeText = '';

  readonly imageStyles: ImageStyleOption[] = [
    { label: 'Rustic',         value: 'rustic'         },
    { label: 'Minimalist',     value: 'minimalist'     },
    { label: 'Mediterranean',  value: 'mediterranean'  },
    { label: 'Cosy',           value: 'cosy'           },
    { label: 'Classic',        value: 'classic'        },
    { label: 'Moody',          value: 'moody'          },
  ];

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

  get formTitle(): string {
    if (this.mode === 'add') return 'New Recipe';
    return this.editTitle ? `Edit ${this.editTitle}` : 'Edit Recipe';
  }

  get imageActionsDisabled(): boolean {
    return this.imageActionInProgress !== null;
  }

  get generateDisabled(): boolean {
    return this.imageActionsDisabled || !this.selectedStyle;
  }

  get idealiseDisabled(): boolean {
    return this.imageActionsDisabled || !this.selectedStyle || !this.currentImageUrl;
  }

  get idealiseTooltip(): string {
    if (!this.currentImageUrl) return 'Upload an image first';
    return '';
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
      prepTimeMinutes: [null as number | null],
      cookTimeMinutes: [null as number | null],
      servings: [null as number | null],
      stages: this.fb.array([this.createStageGroup()])
    });
  }

  private async loadReferenceData(): Promise<void> {
    const [tags] = await Promise.all([
      firstValueFrom(this.api.getTags()),
    ]);
    this.allTags = tags;
    this.unitsService.refresh();
  }

  private loadForEdit(id: number): void {
    Promise.all([
      firstValueFrom(this.api.getRecipe(id)),
      firstValueFrom(this.api.getTags()),
    ]).then(([recipe, tags]) => {
      this.allTags = tags;
      this.unitsService.refresh();
      this.populateForm(recipe);
      this.pageState = 'ready';
    }).catch((err: HttpErrorResponse) => {
      this.pageState = err?.status === 404 ? 'notFound' : 'error';
    });
  }

  private populateForm(recipe: RecipeDetailDto): void {
    this.editTitle = recipe.title;
    this.currentImageUrl = recipe.imageUrl ?? null;
    this.form.patchValue({
      title: recipe.title,
      description: recipe.description ?? '',
      source: recipe.source ?? '',
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
          ing.ingredientId,
          ing.ingredientName,
          ing.amount !== null && ing.amount !== undefined ? +ing.amount : null,
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
    ingredientId: number | null = null,
    ingredientDisplayName = '',
    amount: number | null = null,
    unitId: number | null = null,
    notes = ''
  ): FormGroup {
    return this.fb.group({
      ingredientId: [ingredientId],
      ingredientDisplayName: [ingredientDisplayName],
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
      imageUrl: this.currentImageUrl,
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
          .filter((ing: any) => ing.ingredientId != null)
          .map((ing: any) => ({
            ingredientId: +ing.ingredientId,
            amount: (ing.amount !== null && ing.amount !== '' && ing.amount !== undefined) ? +ing.amount : null,
            unitId: (ing.unitId !== null && ing.unitId !== '') ? +ing.unitId : null,
            notes: ing.notes?.trim() || null
          }))
      }))
    };
  }

  // ─── Image actions ────────────────────────────────────────────────────────

  triggerFileInput(fileInput: HTMLInputElement): void {
    fileInput.value = '';
    fileInput.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file || !this.recipeId) return;

    if (file.size > 5 * 1024 * 1024) {
      this.snackBar.open('Image must be 5 MB or smaller.', undefined, { duration: 4000 });
      return;
    }

    this.imageActionInProgress = 'upload';
    this.api.uploadRecipeImage(this.recipeId, file)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (recipe) => {
          this.currentImageUrl = recipe.imageUrl ?? null;
          this.imageActionInProgress = null;
        },
        error: () => {
          this.imageActionInProgress = null;
          this.snackBar.open('Image upload failed. Please try again.', undefined, { duration: 4000 });
        }
      });
  }

  generateImage(): void {
    if (!this.recipeId || !this.selectedStyle) return;

    const description = (this.form.get('description')?.value ?? '').trim();
    const ingredients = this.getAllIngredientNames();
    const freeText = this.freeText.trim() || undefined;

    this.imageActionInProgress = 'generate';
    this.api.generateRecipeImage(this.recipeId, {
      description,
      ingredients,
      style: this.selectedStyle,
      freeText
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (recipe) => {
          this.currentImageUrl = recipe.imageUrl ?? null;
          this.imageActionInProgress = null;
        },
        error: () => {
          this.imageActionInProgress = null;
          this.snackBar.open('Image generation failed. Please try again.', undefined, { duration: 4000 });
        }
      });
  }

  idealiseImage(): void {
    if (!this.recipeId || !this.selectedStyle || !this.currentImageUrl) return;

    const freeText = this.freeText.trim() || undefined;

    this.imageActionInProgress = 'idealise';
    this.api.idealiseRecipeImage(this.recipeId, {
      style: this.selectedStyle,
      freeText
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (recipe) => {
          this.currentImageUrl = recipe.imageUrl ?? null;
          this.imageActionInProgress = null;
        },
        error: () => {
          this.imageActionInProgress = null;
          this.snackBar.open('Image idealise failed. Please try again.', undefined, { duration: 4000 });
        }
      });
  }

  private getAllIngredientNames(): string[] {
    const names: string[] = [];
    const stages = this.stagesArray.value as any[];
    for (const stage of stages) {
      for (const ing of (stage.ingredients ?? [])) {
        const name = ing.ingredientDisplayName?.trim();
        if (name) names.push(name);
      }
    }
    return names;
  }
}
