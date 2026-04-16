import { Component } from '@angular/core';
import { UserToggleComponent } from './user-toggle/user-toggle.component';
import { SidePanelComponent } from '../side-panel/side-panel.component';
import { SubmitSuggestionDialogComponent } from '../../features/geoff-filter/submit-suggestion-dialog/submit-suggestion-dialog.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [UserToggleComponent, SidePanelComponent, SubmitSuggestionDialogComponent],
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.scss'
})
export class AppHeaderComponent {
  isPanelOpen = false;
  isSuggestionDialogOpen = false;

  openPanel(): void {
    this.isPanelOpen = true;
  }

  closePanel(): void {
    this.isPanelOpen = false;
  }

  onSuggestClick(): void {
    this.isPanelOpen = false;
    this.isSuggestionDialogOpen = true;
  }

  onSuggestionDialogDismissed(): void {
    this.isSuggestionDialogOpen = false;
  }
}
