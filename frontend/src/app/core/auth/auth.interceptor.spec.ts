/// <reference types="vitest/globals" />
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let mockAuthService: {
    accessToken: ReturnType<typeof vi.fn>;
    refresh: ReturnType<typeof vi.fn>;
    clearSession: ReturnType<typeof vi.fn>;
  };
  let mockRouter: {
    navigate: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    mockAuthService = {
      accessToken: vi.fn().mockReturnValue(null),
      refresh: vi.fn(),
      clearSession: vi.fn(),
    };
    mockRouter = {
      navigate: vi.fn().mockResolvedValue(true),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter },
      ],
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controller.verify();
  });

  it('injects Bearer token into Authorization header for non-auth requests', () => {
    mockAuthService.accessToken.mockReturnValue('test-access-token');

    http.get('/api/products').subscribe();

    const req = controller.expectOne('/api/products');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-access-token');
    req.flush({});
  });

  it('adds withCredentials for auth endpoint requests', () => {
    http.post('/api/auth/login', {}).subscribe();

    const req = controller.expectOne('/api/auth/login');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('does not add Authorization header when no token is present', () => {
    mockAuthService.accessToken.mockReturnValue(null);

    http.get('/api/products').subscribe();

    const req = controller.expectOne('/api/products');
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });

  it('retries request with new token after 401 response', () => {
    mockAuthService.accessToken.mockReturnValueOnce('old-token').mockReturnValue('new-token');
    mockAuthService.refresh.mockReturnValue(of(void 0));

    let completed = false;
    http.get('/api/products').subscribe({
      complete: () => {
        completed = true;
      },
    });

    const firstReq = controller.expectOne('/api/products');
    expect(firstReq.request.headers.get('Authorization')).toBe('Bearer old-token');
    firstReq.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(mockAuthService.refresh).toHaveBeenCalled();

    const retryReq = controller.expectOne('/api/products');
    expect(retryReq.request.headers.get('Authorization')).toBe('Bearer new-token');
    retryReq.flush({});

    expect(completed).toBe(true);
  });

  it('clears session and navigates to login when refresh fails after 401', () => {
    mockAuthService.accessToken.mockReturnValue('old-token');
    mockAuthService.refresh.mockReturnValue(throwError(() => new Error('refresh failed')));

    let errored = false;
    http.get('/api/products').subscribe({
      error: () => {
        errored = true;
      },
    });

    const req = controller.expectOne('/api/products');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(mockAuthService.clearSession).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/auth/login']);
    expect(errored).toBe(true);
  });
});
