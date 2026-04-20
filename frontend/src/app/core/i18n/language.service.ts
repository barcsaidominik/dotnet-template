import { Injectable, signal, computed, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly http = inject(HttpClient);

  static readonly SUPPORTED = ['hu-HU', 'en-US'] as const;
  static readonly DEFAULT = 'hu-HU';
  private static readonly STORAGE_KEY = 'preferredLanguage';

  readonly currentLanguage = signal<string>(LanguageService.DEFAULT);
  readonly dateLocale = computed(() => this.currentLanguage());

  initialize(): void {
    const stored = localStorage.getItem(LanguageService.STORAGE_KEY) ?? LanguageService.DEFAULT;
    this.applyLanguage(stored);
  }

  setLanguage(lang: string, syncToServer = true): void {
    if (!LanguageService.SUPPORTED.includes(lang as (typeof LanguageService.SUPPORTED)[number])) {
      return;
    }
    this.applyLanguage(lang);
    if (syncToServer) {
      this.http.put(`${environment.apiUrl}/api/users/me/language`, { language: lang }).subscribe();
    }
  }

  syncFromServer(preferredLanguage: string): void {
    this.applyLanguage(preferredLanguage);
  }

  private applyLanguage(lang: string): void {
    this.currentLanguage.set(lang);
    const langCode = lang === 'hu-HU' ? 'hu' : 'en';
    this.translate.use(langCode);
    localStorage.setItem(LanguageService.STORAGE_KEY, lang);
  }
}
