import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { LanguageService } from '../../core/i18n/language.service';

@Component({
  selector: 'app-language-switcher',
  standalone: true,
  imports: [MatButtonModule],
  template: `
    <button mat-button (click)="toggle()" style="color: white; font-weight: 600;">
      {{ lang.currentLanguage() === 'hu-HU' ? 'EN' : 'HU' }}
    </button>
  `
})
export class LanguageSwitcherComponent {
  readonly lang = inject(LanguageService);

  toggle(): void {
    const next = this.lang.currentLanguage() === 'hu-HU' ? 'en-US' : 'hu-HU';
    this.lang.setLanguage(next);
  }
}
