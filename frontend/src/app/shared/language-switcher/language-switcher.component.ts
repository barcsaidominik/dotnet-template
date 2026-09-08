import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { LanguageService } from '../../core/i18n/language.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [MatButtonModule],
  template: `
    <button mat-flat-button class="language-toggle" (click)="toggle()">
      {{ lang.currentLanguage() === 'hu-HU' ? 'EN' : 'HU' }}
    </button>
  `,
  styles: [
    `
      .language-toggle {
        min-width: 52px;
        padding-inline: 12px;
        font-weight: 800;
        letter-spacing: 0.04em;
        color: #0d47a1 !important;
        background: #ffffff !important;
        border: 1px solid rgba(13, 71, 161, 0.18);
        box-shadow: 0 6px 18px rgba(0, 0, 0, 0.16);
      }
    `,
  ],
})
export class LanguageSwitcherComponent {
  readonly lang = inject(LanguageService);

  toggle(): void {
    const next = this.lang.currentLanguage() === 'hu-HU' ? 'en-US' : 'hu-HU';
    this.lang.setLanguage(next);
  }
}
