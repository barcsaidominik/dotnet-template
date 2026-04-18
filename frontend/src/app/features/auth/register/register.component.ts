import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-register',
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
    .register-page {
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
    .success-message {
      margin-top: 16px;
      padding: 12px;
      background: #e8f5e9;
      border-radius: 4px;
      color: #2e7d32;
    }
  `],
  template: `
    <div class="register-page">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Create Account</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @if (registered()) {
            <div class="success-message">
              Your registration is pending approval by an administrator.
            </div>
            <div class="actions">
              <button mat-button routerLink="/auth/login">Back to Sign In</button>
            </div>
          } @else {
            <form [formGroup]="form" (ngSubmit)="submit()">
              <mat-form-field appearance="outline">
                <mat-label>Email</mat-label>
                <input matInput type="email" formControlName="email" autocomplete="email">
                @if (form.get('email')?.hasError('required') && form.get('email')?.touched) {
                  <mat-error>Email is required</mat-error>
                }
                @if (form.get('email')?.hasError('email') && form.get('email')?.touched) {
                  <mat-error>Invalid email address</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Password</mat-label>
                <input matInput type="password" formControlName="password" autocomplete="new-password">
                @if (form.get('password')?.hasError('required') && form.get('password')?.touched) {
                  <mat-error>Password is required</mat-error>
                }
                @if (form.get('password')?.hasError('minlength') && form.get('password')?.touched) {
                  <mat-error>Password must be at least 6 characters</mat-error>
                }
              </mat-form-field>

              @if (isLoading()) {
                <div class="spinner-wrap">
                  <mat-spinner diameter="32"></mat-spinner>
                </div>
              } @else {
                <div class="actions">
                  <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
                    Register
                  </button>
                  <button mat-button type="button" routerLink="/auth/login">
                    Already have an account?
                  </button>
                </div>
              }
            </form>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `
})
export class RegisterComponent {
  private readonly http = inject(HttpClient);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly isLoading = signal(false);
  readonly registered = signal(false);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  submit(): void {
    if (this.form.invalid) return;
    this.isLoading.set(true);
    const { email, password } = this.form.getRawValue();

    this.http.post(`${environment.apiUrl}/auth/register`, { email, password }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.registered.set(true);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Registration failed. Please try again.', 'Close', { duration: 4000 });
      }
    });
  }
}
