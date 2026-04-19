import { Injectable, signal, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import type { Observable } from 'rxjs';
import { from, map, tap } from 'rxjs';
import { AuthService as GeneratedAuthService } from '../../generated/client/services/auth.service';
import type { TokenResponse } from '../../generated/client/models/token-response';
import { LanguageService } from '../i18n/language.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authApi = inject(GeneratedAuthService);
  private readonly router = inject(Router);
  private readonly langService = inject(LanguageService);

  private readonly _accessToken = signal<string | null>(null);
  private readonly _role = signal<string | null>(null);
  private readonly _tokenExpiry = signal<Date | null>(null);
  private readonly _facilityId = signal<string | null>(null);

  readonly isLoggedIn = computed(() => this._accessToken() !== null);
  readonly role = computed(() => this._role());
  readonly facilityId = computed(() => this._facilityId());
  readonly isSystemAdmin = computed(() => this._role() === 'SystemAdmin');
  readonly isFacilityAdmin = computed(() => this._role() === 'FacilityAdmin');
  readonly canEditProducts = computed(() =>
    ['FacilityAdmin', 'FacilityEditor'].includes(this._role() ?? '')
  );
  readonly accessToken = computed(() => this._accessToken());

  login(email: string, password: string): Observable<TokenResponse> {
    return from(this.authApi.apiAuthLoginPost$Json({ body: { email, password } }));
  }

  register(email: string, password: string) {
    return from(this.authApi.apiAuthRegisterPost({ body: { email, password } }));
  }

  setSession(response: TokenResponse): void {
    this._accessToken.set(response.token);
    this._role.set(response.role);
    this._tokenExpiry.set(new Date(response.expiresAt));

    try {
      const payload = JSON.parse(atob(response.token.split('.')[1]));
      this._facilityId.set(payload['facilityId'] ?? null);
    } catch {
      this._facilityId.set(null);
    }

    if (response.preferredLanguage) {
      this.langService.syncFromServer(response.preferredLanguage);
    }
  }

  refresh(): Observable<void> {
    return from(this.authApi.apiAuthRefreshPost$Json()).pipe(
      tap((response) => this.setSession(response)),
      map(() => void 0)
    );
  }

  setPassword(email: string, token: string, newPassword: string) {
    return from(this.authApi.apiAuthSetPasswordPost({ body: { email, token, newPassword } }));
  }

  logout(): void {
    from(this.authApi.apiAuthLogoutPost()).subscribe({
      complete: () => {
        this._accessToken.set(null);
        this._role.set(null);
        this._tokenExpiry.set(null);
        this._facilityId.set(null);
        this.router.navigate(['/auth/login']);
      },
    });
  }

  clearSession(): void {
    this._accessToken.set(null);
    this._role.set(null);
    this._tokenExpiry.set(null);
    this._facilityId.set(null);
  }
}
