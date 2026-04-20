import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private static readonly STORAGE_KEY = 'darkMode';

  readonly isDark = signal(false);

  initialize(): void {
    const stored = localStorage.getItem(ThemeService.STORAGE_KEY);
    const isDark = stored === 'true';
    this.isDark.set(isDark);
    this.applyTheme(isDark);
  }

  toggle(): void {
    const next = !this.isDark();
    this.isDark.set(next);
    this.applyTheme(next);
    localStorage.setItem(ThemeService.STORAGE_KEY, String(next));
  }

  private applyTheme(isDark: boolean): void {
    if (isDark) {
      document.documentElement.classList.add('dark-mode');
    } else {
      document.documentElement.classList.remove('dark-mode');
    }
  }
}
