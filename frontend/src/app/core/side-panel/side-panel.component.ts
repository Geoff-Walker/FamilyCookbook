import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-side-panel',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './side-panel.component.html',
  styleUrl: './side-panel.component.scss'
})
export class SidePanelComponent {
  @Input() isOpen = false;
  @Output() closed = new EventEmitter<void>();

  close(): void {
    this.closed.emit();
  }

  onSuggestClick(): void {
    // WAL-82: Submit Suggestion modal — not yet implemented
    console.log('Submit Suggestion tapped — modal coming in WAL-82');
  }
}
