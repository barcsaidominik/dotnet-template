/// <reference types="vitest/globals" />
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import type { UrlTree } from '@angular/router';
import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  it('allows navigation when the user is logged in', () => {
    const auth = {
      isLoggedIn: vi.fn().mockReturnValue(true),
    };
    const router = {
      createUrlTree: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });

    const result = TestBed.runInInjectionContext(() => authGuard(null as never, null as never));

    expect(result).toBe(true);
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects anonymous users to the login page', () => {
    const loginTree = {} as UrlTree;
    const auth = {
      isLoggedIn: vi.fn().mockReturnValue(false),
    };
    const router = {
      createUrlTree: vi.fn().mockReturnValue(loginTree),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });

    const result = TestBed.runInInjectionContext(() => authGuard(null as never, null as never));

    expect(result).toBe(loginTree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/auth/login']);
  });
});
