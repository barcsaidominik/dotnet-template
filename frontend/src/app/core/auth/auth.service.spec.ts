import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import type { TokenResponse } from '../../generated/client/models/token-response';
import { AuthService as GeneratedAuthService } from '../../generated/client/services/auth.service';
import { LanguageService } from '../i18n/language.service';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let authApi: {
    apiAuthLoginPost$Json: ReturnType<typeof vi.fn>;
    apiAuthRegisterPost: ReturnType<typeof vi.fn>;
    apiAuthRefreshPost$Json: ReturnType<typeof vi.fn>;
    apiAuthSetPasswordPost: ReturnType<typeof vi.fn>;
    apiAuthLogoutPost: ReturnType<typeof vi.fn>;
  };
  let router: {
    navigate: ReturnType<typeof vi.fn>;
  };
  let languageService: {
    syncFromServer: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    authApi = {
      apiAuthLoginPost$Json: vi.fn(),
      apiAuthRegisterPost: vi.fn(),
      apiAuthRefreshPost$Json: vi.fn(),
      apiAuthSetPasswordPost: vi.fn(),
      apiAuthLogoutPost: vi.fn(),
    };
    router = {
      navigate: vi.fn().mockResolvedValue(true),
    };
    languageService = {
      syncFromServer: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        AuthService,
        {
          provide: GeneratedAuthService,
          useValue: authApi,
        },
        {
          provide: Router,
          useValue: router,
        },
        {
          provide: LanguageService,
          useValue: languageService,
        },
      ],
    });

    service = TestBed.inject(AuthService);
  });

  it('setSession stores token, role, expiry and decoded facility id', () => {
    const response = createTokenResponse({
      role: 'FacilityAdmin',
      preferredLanguage: 'en-US',
      payload: { facilityId: 'facility-123' },
    });

    service.setSession(response);

    expect(service.isLoggedIn()).toBe(true);
    expect(service.role()).toBe('FacilityAdmin');
    expect(service.facilityId()).toBe('facility-123');
    expect(service.isFacilityAdmin()).toBe(true);
    expect(service.canEditProducts()).toBe(true);
    expect(languageService.syncFromServer).toHaveBeenCalledWith('en-US');
  });

  it('setSession clears facility id when token payload cannot be decoded', () => {
    service.setSession({
      token: 'invalid-token',
      expiresAt: '2026-04-19T21:00:00Z',
      role: 'FacilityViewer',
      preferredLanguage: 'hu-HU',
    });

    expect(service.facilityId()).toBeNull();
    expect(service.canEditProducts()).toBe(false);
  });

  it('refresh updates the stored session from backend response', async () => {
    const response = createTokenResponse({
      role: 'SystemAdmin',
      preferredLanguage: 'hu-HU',
    });
    authApi.apiAuthRefreshPost$Json.mockResolvedValue(response);

    await new Promise<void>((resolve, reject) => {
      service.refresh().subscribe({
        next: () => resolve(),
        error: reject,
      });
    });

    expect(service.isLoggedIn()).toBe(true);
    expect(service.isSystemAdmin()).toBe(true);
    expect(service.role()).toBe('SystemAdmin');
  });

  it('logout clears session and redirects to the login page', async () => {
    service.setSession(
      createTokenResponse({
        role: 'FacilityAdmin',
        preferredLanguage: 'hu-HU',
        payload: { facilityId: 'facility-123' },
      })
    );
    authApi.apiAuthLogoutPost.mockResolvedValue(undefined);

    service.logout();
    await Promise.resolve();

    expect(service.isLoggedIn()).toBe(false);
    expect(service.role()).toBeNull();
    expect(service.facilityId()).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
  });

  it('login proxies the generated api call payload', async () => {
    const response = createTokenResponse({
      role: 'FacilityAdmin',
      preferredLanguage: 'hu-HU',
    });
    authApi.apiAuthLoginPost$Json.mockResolvedValue(response);

    const result = await new Promise<TokenResponse>((resolve, reject) => {
      service.login('user@test.local', 'Secret123!').subscribe({
        next: resolve,
        error: reject,
      });
    });

    expect(authApi.apiAuthLoginPost$Json).toHaveBeenCalledWith({
      body: { email: 'user@test.local', password: 'Secret123!' },
    });
    expect(result).toEqual(response);
  });

  function createTokenResponse(options: {
    role: string;
    preferredLanguage: string;
    payload?: Record<string, string>;
  }): TokenResponse {
    const payload = {
      sub: 'user-1',
      ...options.payload,
    };

    return {
      token: createJwt(payload),
      expiresAt: '2026-04-19T21:00:00Z',
      role: options.role,
      preferredLanguage: options.preferredLanguage,
    };
  }

  function createJwt(payload: Record<string, string>): string {
    const encode = (value: object) =>
      btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');

    return `${encode({ alg: 'HS256', typ: 'JWT' })}.${encode(payload)}.signature`;
  }
});
