import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { AuthLoginComponent } from './login.component';

describe('AuthLoginComponent', () => {
  let auth: {
    login: ReturnType<typeof vi.fn>;
    setSession: ReturnType<typeof vi.fn>;
  };
  let router: {
    navigate: ReturnType<typeof vi.fn>;
  };
  let snackBar: {
    open: ReturnType<typeof vi.fn>;
  };
  let translate: {
    instant: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    auth = { login: vi.fn(), setSession: vi.fn() };
    router = { navigate: vi.fn().mockResolvedValue(true) };
    snackBar = { open: vi.fn() };
    translate = { instant: vi.fn((key: string) => key) };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
      ],
    });
  });

  it('does nothing when form is invalid', () => {
    const component = TestBed.runInInjectionContext(() => new AuthLoginComponent());

    component.submit();

    expect(auth.login).not.toHaveBeenCalled();
    expect(component.isLoading()).toBe(false);
  });

  it('calls login and navigates to /admin/users for SystemAdmin', () => {
    auth.login.mockReturnValue(
      of({ token: 'tok', role: 'SystemAdmin', expiresAt: '2099-01-01T00:00:00Z' })
    );
    const component = TestBed.runInInjectionContext(() => new AuthLoginComponent());
    component.form.setValue({ email: 'admin@test.com', password: 'secret' });

    component.submit();

    expect(auth.login).toHaveBeenCalledWith('admin@test.com', 'secret');
    expect(auth.setSession).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/users']);
  });

  it('navigates to /products for non-admin roles', () => {
    auth.login.mockReturnValue(
      of({ token: 'tok', role: 'FacilityAdmin', expiresAt: '2099-01-01T00:00:00Z' })
    );
    const component = TestBed.runInInjectionContext(() => new AuthLoginComponent());
    component.form.setValue({ email: 'user@test.com', password: 'pass' });

    component.submit();

    expect(router.navigate).toHaveBeenCalledWith(['/products']);
  });

  it('shows invalid credentials snackbar on login error', () => {
    auth.login.mockReturnValue(throwError(() => new Error('401')));
    const component = TestBed.runInInjectionContext(() => new AuthLoginComponent());
    component.form.setValue({ email: 'user@test.com', password: 'wrong' });

    component.submit();

    expect(component.isLoading()).toBe(false);
    expect(snackBar.open).toHaveBeenCalledWith('auth.login.invalidCredentials', 'common.close', {
      duration: 4000,
    });
  });

  it('shows navigationFailed snackbar when router.navigate rejects', async () => {
    auth.login.mockReturnValue(
      of({ token: 'tok', role: 'FacilityEditor', expiresAt: '2099-01-01T00:00:00Z' })
    );
    router.navigate.mockRejectedValue(new Error('nav error'));
    const component = TestBed.runInInjectionContext(() => new AuthLoginComponent());
    component.form.setValue({ email: 'user@test.com', password: 'pass' });

    component.submit();
    await Promise.resolve();
    await Promise.resolve();

    expect(component.isLoading()).toBe(false);
    expect(snackBar.open).toHaveBeenCalledWith('auth.login.navigationFailed', 'common.close', {
      duration: 4000,
    });
  });
});
