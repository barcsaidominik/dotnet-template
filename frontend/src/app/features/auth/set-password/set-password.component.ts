import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { environment } from '../../../../environments/environment';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('newPassword');
  const confirm = control.get('confirmPassword');
  if (password && confirm && password.value !== confirm.value) {
    return { passwordMismatch: true };
  }
  return null;
}

@Component({
  selector: 'app-set-password',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  styles: [`
    .set-password-page {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: #f5f5f5;
    }
    mat-card {
      width: 100%;
      max-width: 400px;
      padding: 8px;
    }
    mat-form-field {
      width: 100%;
    }
    .actions {
      display: flex;
      flex-direction: column;
      gap: 8px;
      margin-top: 8px;
    }
    .spinner-wrap {
      display: flex;
      justify-content: center;
      padding: 8px 0;
    }
  `],
  template: `
    <div class="set-password-page">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Set Password</mat-card-title>
          <mat-card-subtitle>Complete your account setup</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()">
            <mat-form-field appearance="outline">
              <mat-label>Email</mat-label>
              <input matInput type="email" formControlName="email" autocomplete="email">
              @if (form.get('email')?.hasError('required') && form.get('email')?.touched) {
                <mat-error>Email is required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Setup Token</mat-label>
              <input matInput formControlName="token">
              @if (form.get('token')?.hasError('required') && form.get('token')?.touched) {
                <mat-error>Token is required</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>New Password</mat-label>
              <input matInput type="password" formControlName="newPassword" autocomplete="new-password">
              @if (form.get('newPassword')?.hasError('required') && form.get('newPassword')?.touched) {
                <mat-error>Password is required</mat-error>
              }
              @if (form.get('newPassword')?.hasError('minlength') && form.get('newPassword')?.touched) {
                <mat-error>Password must be at least 6 characters</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Confirm Password</mat-label>
              <input matInput type="password" formControlName="confirmPassword" autocomplete="new-password">
              @if (form.hasError('passwordMismatch') && form.get('confirmPassword')?.touched) {
                <mat-error>Passwords do not match</mat-error>
              }
            </mat-form-field>

            @if (isLoading()) {
              <div class="spinner-wrap">
                <mat-spinner diameter="32"></mat-spinner>
              </div>
            } @else {
              <div class="actions">
                <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
                  Set Password
                </button>
                <button mat-button type="button" routerLink="/auth/login">
                  Back to Sign In
                </button>
              </div>
            }
          </form>
        </mat-card-content>
      </mat-card>
    </div>
  `
})
export class SetPasswordComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly isLoading = signal(false);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    token: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required],
  }, { validators: passwordMatchValidator });

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email') ?? '';
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (email) this.form.get('email')?.setValue(email);
    if (token) this.form.get('token')?.setValue(token);
  }

  submit(): void {
    if (this.form.invalid) return;
    this.isLoading.set(true);
    const { email, token, newPassword } = this.form.getRawValue();

    this.http.post(`${environment.apiUrl}/auth/set-password`, { email, token, newPassword }).subscribe({
      next: () => {
        this.snackBar.open('Password set successfully. Please sign in.', 'Close', { duration: 4000 });
        this.router.navigate(['/auth/login']);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to set password. Check your token and try again.', 'Close', { duration: 4000 });
      }
    });
  }
}
