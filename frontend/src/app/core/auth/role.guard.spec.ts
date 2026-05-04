/// <reference types="vitest/globals" />
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import type { UrlTree } from '@angular/router';
import { AuthService } from './auth.service';
import { roleGuard } from './role.guard';

describe('roleGuard', () => {
  it('allows navigation when the current role is explicitly allowed', () => {
    const auth = {
      role: vi.fn().mockReturnValue('FacilityEditor'),
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

    const result = TestBed.runInInjectionContext(() =>
      roleGuard(['FacilityAdmin', 'FacilityEditor'])(null as never, null as never)
    );

    expect(result).toBe(true);
  });

  it('redirects system admins to their home area when the role is not allowed', () => {
    const tree = {} as UrlTree;
    const auth = {
      role: vi.fn().mockReturnValue('SystemAdmin'),
    };
    const router = {
      createUrlTree: vi.fn().mockReturnValue(tree),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });

    const result = TestBed.runInInjectionContext(() =>
      roleGuard(['FacilityAdmin'])(null as never, null as never)
    );

    expect(result).toBe(tree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/admin/users']);
  });

  it('redirects facility admins to their home area when the role is not allowed', () => {
    const tree = {} as UrlTree;
    const auth = {
      role: vi.fn().mockReturnValue('FacilityAdmin'),
    };
    const router = {
      createUrlTree: vi.fn().mockReturnValue(tree),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });

    const result = TestBed.runInInjectionContext(() =>
      roleGuard(['SystemAdmin'])(null as never, null as never)
    );

    expect(result).toBe(tree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/facility/users']);
  });

  it('redirects other users to the products page when no special role matches', () => {
    const tree = {} as UrlTree;
    const auth = {
      role: vi.fn().mockReturnValue('FacilityViewer'),
    };
    const router = {
      createUrlTree: vi.fn().mockReturnValue(tree),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });

    const result = TestBed.runInInjectionContext(() =>
      roleGuard(['SystemAdmin'])(null as never, null as never)
    );

    expect(result).toBe(tree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/products']);
  });
});
