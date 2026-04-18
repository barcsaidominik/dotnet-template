import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { LoginResponse } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

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
    ['FacilityAdmin', 'FacilityEditor'].includes(this._role() ?? ''));
  readonly accessToken = computed(() => this._accessToken());

  login(email: string, password: string) {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`,
      { email, password },
      { withCredentials: true }
    );
  }

  setSession(response: LoginResponse): void {
    this._accessToken.set(response.token);
    this._role.set(response.role);
    this._tokenExpiry.set(new Date(response.expiresAt));

    try {
      const payload = JSON.parse(atob(response.token.split('.')[1]));
      this._facilityId.set(payload['facilityId'] ?? null);
    } catch {
      this._facilityId.set(null);
    }
  }

  refresh() {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/refresh`,
      {},
      { withCredentials: true }
    );
  }

  logout(): void {
    this.http.post(`${environment.apiUrl}/auth/logout`, {},
      { withCredentials: true }
    ).subscribe({
      complete: () => {
        this._accessToken.set(null);
        this._role.set(null);
        this._tokenExpiry.set(null);
        this._facilityId.set(null);
        this.router.navigate(['/auth/login']);
      }
    });
  }

  clearSession(): void {
    this._accessToken.set(null);
    this._role.set(null);
    this._tokenExpiry.set(null);
    this._facilityId.set(null);
  }
}
