import type { ApplicationConfig } from '@angular/core';
import { APP_INITIALIZER } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeHu from '@angular/common/locales/hu';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { firstValueFrom, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { AuthService } from './core/auth/auth.service';
import { LanguageService } from './core/i18n/language.service';
import { ThemeService } from './core/theme/theme.service';
import { provideApiConfiguration } from './generated/client/api-configuration';
import { provideApiConfiguration as provideProductsApiConfiguration } from './generated/products-client/api-configuration';
import { environment } from '../environments/environment';

registerLocaleData(localeHu, 'hu-HU');

function initializeAuth(auth: AuthService) {
  return () => firstValueFrom(auth.refresh().pipe(catchError(() => of(null))));
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withXhr(), withInterceptors([authInterceptor])),
    provideApiConfiguration(environment.apiUrl),
    provideProductsApiConfiguration(environment.apiUrl),
    provideAnimationsAsync(),
    provideTranslateService({
      fallbackLang: 'hu',
      // useHttpBackend bypasses the HttpClient interceptor chain (authInterceptor),
      // which otherwise re-enters AuthService -> LanguageService -> TranslateService
      // construction and triggers NG0200 (circular DI). The i18n JSON assets are
      // static and unauthenticated, so they have no business going through auth.
      loader: provideTranslateHttpLoader({
        prefix: './assets/i18n/',
        suffix: '.json',
        useHttpBackend: true,
      }),
    }),
    {
      provide: APP_INITIALIZER,
      useFactory: (auth: AuthService) => initializeAuth(auth),
      deps: [AuthService],
      multi: true,
    },
    {
      provide: APP_INITIALIZER,
      useFactory: (lang: LanguageService) => () => lang.initialize(),
      deps: [LanguageService],
      multi: true,
    },
    {
      provide: APP_INITIALIZER,
      useFactory: (theme: ThemeService) => () => theme.initialize(),
      deps: [ThemeService],
      multi: true,
    },
  ],
};
