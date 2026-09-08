/// <reference types="vitest/globals" />
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TranslateService } from '@ngx-translate/core';
import { appConfig } from './app.config';
import { AuthService } from './core/auth/auth.service';
import { AuthService as GeneratedAuthService } from './generated/client/services/auth.service';

/**
 * Regression test for NG0200 ("Circular dependency in DI detected").
 *
 * The DI graph is: TranslateService's constructor eagerly loads its
 * fallback language via the translate http loader -> HttpClient -> the real
 * authInterceptor -> AuthService -> LanguageService -> TranslateService.
 * If the translate loader shares the intercepted HttpClient, constructing
 * AuthService (exactly what the first APP_INITIALIZER in app.config.ts does)
 * re-enters TranslateService construction and Angular throws NG0200.
 *
 * This spec wires up the *real* appConfig.providers (not a hand-picked
 * subset) so any future change that reintroduces the cycle is caught here.
 *
 * The generated `AuthService` (ng-openapi-gen swagger client) is stubbed
 * out, same as in auth.service.spec.ts: it uses classic constructor-param
 * DI, which this project's JIT-only Vitest setup (no emitDecoratorMetadata)
 * cannot resolve for project-local classes. That is an unrelated,
 * pre-existing test-environment limitation, not part of the DI cycle under
 * test here.
 */
describe('appConfig DI graph', () => {
  let controller: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        ...appConfig.providers,
        provideHttpClientTesting(),
        {
          provide: GeneratedAuthService,
          // The first APP_INITIALIZER (initializeAuth) calls auth.refresh()
          // synchronously during TestBed module finalization; give it a
          // rejecting call so it resolves via the existing catchError(() =>
          // of(null)) in app.config.ts instead of throwing.
          useValue: { apiAuthRefreshPost$Json: () => Promise.reject(new Error('no session')) },
        },
      ],
    });
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controller.verify();
    localStorage.clear();
  });

  it('constructs the AuthService -> LanguageService -> TranslateService chain without NG0200, loads a translation via the http loader, and resolves real keys through instant()', () => {
    // Mirrors the real bootstrap order: the first APP_INITIALIZER injects
    // AuthService, which cascades into LanguageService and TranslateService.
    // If the cycle regresses, this throws NG0200 right here.
    expect(() => TestBed.inject(AuthService)).not.toThrow();
    const translate = TestBed.inject(TranslateService);

    // TranslateService's constructor eagerly loads the configured fallback
    // language ('hu'), proving the loader can actually perform an HTTP call.
    const req = controller.expectOne('./assets/i18n/hu.json');
    expect(req.request.method).toBe('GET');
    req.flush({ auth: { login: { title: 'Bejelentkezés' } } });

    translate.use('hu');

    expect(translate.instant('auth.login.title')).toBe('Bejelentkezés');
    expect(translate.instant('auth.login.title')).not.toBe('auth.login.title');
  });
});
