/// <reference types="vitest/globals" />
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';
import { LanguageService } from './language.service';

describe('LanguageService', () => {
  let service: LanguageService;
  let httpTestingController: HttpTestingController;
  let translateService: { use: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    localStorage.clear();
    translateService = {
      use: vi.fn().mockReturnValue(of({})),
    };

    TestBed.configureTestingModule({
      providers: [
        LanguageService,
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: TranslateService,
          useValue: translateService,
        },
      ],
    });

    service = TestBed.inject(LanguageService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
    localStorage.clear();
  });

  it('initialize uses stored language when available', async () => {
    localStorage.setItem('preferredLanguage', 'en-US');

    await service.initialize();

    expect(service.currentLanguage()).toBe('en-US');
    expect(translateService.use).toHaveBeenCalledWith('en');
  });

  it('setLanguage persists language locally and syncs it to the backend by default', () => {
    service.setLanguage('en-US');

    expect(service.currentLanguage()).toBe('en-US');
    expect(localStorage.getItem('preferredLanguage')).toBe('en-US');
    expect(translateService.use).toHaveBeenCalledWith('en');

    const request = httpTestingController.expectOne('/api/users/me/language');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ language: 'en-US' });
    request.flush({});
  });

  it('setLanguage skips backend sync when explicitly disabled', () => {
    service.setLanguage('en-US', false);

    expect(service.currentLanguage()).toBe('en-US');
    httpTestingController.expectNone('/api/users/me/language');
  });

  it('ignores unsupported languages', () => {
    service.setLanguage('de-DE');

    expect(service.currentLanguage()).toBe(LanguageService.DEFAULT);
    expect(translateService.use).not.toHaveBeenCalled();
    httpTestingController.expectNone('/api/users/me/language');
  });
});
