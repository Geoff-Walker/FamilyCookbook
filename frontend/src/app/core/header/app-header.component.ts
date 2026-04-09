import { Component } from '@angular/core';
import { UserToggleComponent } from './user-toggle/user-toggle.component';
import { SidePanelComponent } from '../side-panel/side-panel.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [UserToggleComponent, SidePanelComponent],
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.scss'
})
export class AppHeaderComponent {
  isPanelOpen = false;

  openPanel(): void {
    this.isPanelOpen = true;
  }

  closePanel(): void {
    this.isPanelOpen = false;
  }
}
