import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-promote-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './promote-confirm-dialog.component.html',
  styleUrl: './promote-confirm-dialog.component.scss'
})
export class PromoteConfirmDialogComponent {
  /** Whether the promote API call is in flight. */
  @Input() isPromoting = false;

  /** Emitted when the user confirms the promote action. */
  @Output() confirmed = new EventEmitter<void>();

  /** Emitted when the user cancels or dismisses the dialog. */
  @Output() cancelled = new EventEmitter<void>();

  onConfirm(): void {
    this.confirmed.emit();
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  onBackdropClick(): void {
    if (!this.isPromoting) {
      this.cancelled.emit();
    }
  }

  /** Dismiss on Escape unless a promote call is in flight. */
  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.isPromoting) {
      this.cancelled.emit();
    }
  }
}
