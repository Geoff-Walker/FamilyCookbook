import { Component } from '@angular/core';

@Component({
  selector: 'app-geoff-filter',
  standalone: true,
  imports: [],
  template: `
    <div class="shell-page">
      <h1>The Geoff Filter</h1>
      <p>Coming soon — WAL-64.</p>
    </div>
  `,
  styles: [`
    .shell-page {
      padding: var(--space-8) var(--space-6);
    }
  `]
})
export class GeoffFilterComponent {}
