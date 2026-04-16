import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnInit,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ShoppingItem {
  ingredientId: number;
  ingredientName: string;
  unitId: number | null;
  unitName: string | null;
  unitAbbreviation: string | null;
  totalAmount: number | null;
}

/**
 * Ephemeral shopping list sheet.
 * - Tablet/desktop: right-side drawer (360px, position: fixed, slides in from right).
 * - Mobile: centred dialog overlay.
 * All ticked state is component-local — never persisted.
 */
@Component({
  selector: 'app-shopping-list-sheet',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './shopping-list-sheet.component.html',
  styleUrl: './shopping-list-sheet.component.scss'
})
export class ShoppingListSheetComponent implements OnInit {
  @Input({ required: true }) items: ShoppingItem[] = [];
  @Input({ required: true }) windowRangeLabel!: string;
  @Input() ifItsCount = 0;

  /** Emitted when the user closes the sheet. */
  @Output() closed = new EventEmitter<void>();

  /** Tracks which ingredientId+unitId keys are ticked. */
  checkedKeys = new Set<string>();

  /** Animate in on init. */
  visible = false;

  ngOnInit(): void {
    // Trigger slide-in animation on next frame
    requestAnimationFrame(() => {
      this.visible = true;
    });
  }

  itemKey(item: ShoppingItem): string {
    return `${item.ingredientId}:${item.unitId ?? 'none'}`;
  }

  isChecked(item: ShoppingItem): boolean {
    return this.checkedKeys.has(this.itemKey(item));
  }

  toggleCheck(item: ShoppingItem): void {
    const key = this.itemKey(item);
    if (this.checkedKeys.has(key)) {
      this.checkedKeys.delete(key);
    } else {
      this.checkedKeys.add(key);
    }
    // Trigger change detection by creating a new Set reference
    this.checkedKeys = new Set(this.checkedKeys);
  }

  formatQuantity(item: ShoppingItem): string {
    if (item.totalAmount == null) return '';
    const abbr = item.unitAbbreviation ?? item.unitName ?? '';
    // Round to 2 decimal places, strip trailing zeros
    const rounded = parseFloat(item.totalAmount.toFixed(2));
    return abbr ? `${rounded} ${abbr}` : String(rounded);
  }

  close(): void {
    this.visible = false;
    // Allow the slide-out animation before emitting
    setTimeout(() => this.closed.emit(), 300);
  }

  onBackdropClick(): void {
    this.close();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close();
  }
}
