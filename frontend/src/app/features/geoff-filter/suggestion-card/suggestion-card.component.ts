import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { RecipeSuggestionDto } from '../../../core/models/geoff-filter.models';

export type CardTab = 'queue' | 'backlog';

export interface AcceptEvent { suggestionId: number; }
export interface BacklogEvent { suggestionId: number; }
export interface DeleteEvent  { suggestionId: number; }

@Component({
  selector: 'app-suggestion-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './suggestion-card.component.html',
  styleUrl: './suggestion-card.component.scss'
})
export class SuggestionCardComponent implements OnChanges {
  @Input({ required: true }) suggestion!: RecipeSuggestionDto;
  @Input({ required: true }) activeUserId!: number;
  @Input({ required: true }) tab!: CardTab;
  /** Inline accept error driven by parent (e.g. 403 response). */
  @Input() acceptError: string | null = null;

  @Output() accept = new EventEmitter<AcceptEvent>();
  @Output() backlog = new EventEmitter<BacklogEvent>();
  @Output() delete  = new EventEmitter<DeleteEvent>();

  /** Whether the card is fading out before removal. */
  fading = false;

  ngOnChanges(): void {
    // intentionally left for future use
  }

  get isGeoff(): boolean {
    return this.activeUserId === 1;
  }

  get isAccepted(): boolean {
    return this.suggestion.status === 'accepted';
  }

  get formattedDate(): string {
    const d = new Date(this.suggestion.createdAt);
    return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  get submitterName(): string {
    return this.suggestion.submittedByUserName;
  }

  get submitterIsGeoff(): boolean {
    return this.suggestion.submittedByUserId === 1;
  }

  onAccept(): void {
    this.accept.emit({ suggestionId: this.suggestion.id });
  }

  onBacklog(): void {
    this.backlog.emit({ suggestionId: this.suggestion.id });
  }

  onDelete(): void {
    this.fading = true;
    // Parent will remove the card after the animation; emit after a short delay
    // so the animation can play. The parent listens and removes from list.
    setTimeout(() => this.delete.emit({ suggestionId: this.suggestion.id }), 300);
  }
}
