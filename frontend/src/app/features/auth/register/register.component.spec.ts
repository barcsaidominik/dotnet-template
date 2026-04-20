import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { AuthRegisterComponent } from './register.component';

describe('AuthRegisterComponent', () => {
  let auth: {
    register: ReturnType<typeof vi.fn>;
  };
  let snackBar: {
    open: ReturnType<typeof vi.fn>;
  };
  let translate: {
    instant: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    auth = { register: vi.fn() };
    snackBar = { open: vi.fn() };
    translate = { instant: vi.fn((key: string) => key) };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
      ],
    });
  });

  it('does nothing when form is invalid', () => {
    const component = TestBed.runInInjectionContext(() => new AuthRegisterComponent());

    component.submit();

    expect(auth.register).not.toHaveBeenCalled();
    expect(component.isLoading()).toBe(false);
  });

  it('calls register with form values and sets registered flag on success', () => {
    auth.register.mockReturnValue(of(undefined));
    const component = TestBed.runInInjectionContext(() => new AuthRegisterComponent());
    component.form.setValue({ email: 'user@test.com', password: 'pass123' });

    component.submit();

    expect(auth.register).toHaveBeenCalledWith('user@test.com', 'pass123');
    expect(component.registered()).toBe(true);
    expect(component.isLoading()).toBe(false);
  });

  it('shows error snackbar on registration failure', () => {
    auth.register.mockReturnValue(throwError(() => new Error('500')));
    const component = TestBed.runInInjectionContext(() => new AuthRegisterComponent());
    component.form.setValue({ email: 'user@test.com', password: 'pass123' });

    component.submit();

    expect(component.isLoading()).toBe(false);
    expect(snackBar.open).toHaveBeenCalledWith('auth.register.failed', 'common.close', {
      duration: 4000,
    });
  });
});
