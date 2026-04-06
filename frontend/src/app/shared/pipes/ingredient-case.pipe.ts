import { Pipe, PipeTransform } from '@angular/core';

/**
 * IngredientCasePipe
 *
 * Transforms ingredient names for display: title-case the string, but keep
 * common short words lowercase unless they appear as the first word.
 *
 * Examples:
 *   "olive oil"        → "Olive Oil"
 *   "salt and pepper"  → "Salt and Pepper"
 *   "juice of a lemon" → "Juice of a Lemon"
 */

const LOWERCASE_WORDS = new Set([
  'and', 'or', 'of', 'with', 'a', 'an', 'the', 'in', 'on', 'at', 'to', 'for'
]);

@Pipe({
  name: 'ingredientCase',
  standalone: true,
  pure: true
})
export class IngredientCasePipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) return '';

    return value
      .trim()
      .split(/\s+/)
      .map((word, index) => {
        if (!word) return word;
        // Always capitalise the first word; lowercase the rest if they are
        // in the exception list, otherwise title-case them.
        const lower = word.toLowerCase();
        if (index === 0 || !LOWERCASE_WORDS.has(lower)) {
          return lower.charAt(0).toUpperCase() + lower.slice(1);
        }
        return lower;
      })
      .join(' ');
  }
}
