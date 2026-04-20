import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { AuthSetPasswordComponent } from './set-password.component';

function makeRoute(params: Record<string, string> = {}) {
  return {
    snapshot: {
      queryParamMap: {
        get: (key: string) => params[key] ?? null,
      },
    },
  };
}

describe('AuthSetPasswordComponent', () => {
  let auth: {
    setPassword: ReturnType<typeof vi.fn>;
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
    auth = { setPassword: vi.fn() };
    router = { navigate: vi.fn().mockResolvedValue(true) };
    snackBar = { open: vi.fn() };
    translate = { instant: vi.fn((key: string) => key) };
  });

  function setup(params: Record<string, string> = {}) {
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: makeRoute(params) },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
      ],
    });
    return TestBed.runInInjectionContext(() => new AuthSetPasswordComponent());
  }

  it('pre-fills email and token from query params on init', () => {
    const component = setup({ email: 'user@test.com', token: 'abc123' });

    component.ngOnInit();

    expect(component.form.get('email')?.value).toBe('user@test.com');
    expect(component.form.get('token')?.value).toBe('abc123');
  });

  it('leaves fields empty when no query params present', () => {
    const component = setup();

    component.ngOnInit();

    expect(component.form.get('email')?.value).toBe('');
    expect(component.form.get('token')?.value).toBe('');
  });

  it('does nothing when form is invalid', () => {
    const component = setup();

    component.submit();

    expect(auth.setPassword).not.toHaveBeenCalled();
  });

  it('calls setPassword and navigates to /auth/login on success', () => {
    auth.setPassword.mockReturnValue(of(undefined));
    const component = setup();
    component.form.setValue({
      email: 'user@test.com',
      token: 'tok',
      newPassword: 'newpass1',
      confirmPassword: 'newpass1',
    });

    component.submit();

    expect(auth.setPassword).toHaveBeenCalledWith('user@test.com', 'tok', 'newpass1');
    expect(snackBar.open).toHaveBeenCalledWith('auth.setPassword.success', 'common.close', {
      duration: 4000,
    });
    expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
  });

  it('shows error snackbar and clears loading state on failure', () => {
    auth.setPassword.mockReturnValue(throwError(() => new Error('400')));
    const component = setup();
    component.form.setValue({
      email: 'user@test.com',
      token: 'tok',
      newPassword: 'newpass1',
      confirmPassword: 'newpass1',
    });

    component.submit();

    expect(component.isLoading()).toBe(false);
    expect(snackBar.open).toHaveBeenCalledWith('auth.setPassword.failed', 'common.close', {
      duration: 4000,
    });
  });

  it('form is invalid when passwords do not match', () => {
    const component = setup();
    component.form.setValue({
      email: 'user@test.com',
      token: 'tok',
      newPassword: 'passA',
      confirmPassword: 'passB',
    });

    expect(component.form.errors?.['passwordMismatch']).toBe(true);
  });
});
