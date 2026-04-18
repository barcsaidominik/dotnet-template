import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { FacilityService } from '../../../core/services/facility.service';
import { AuthService } from '../../../core/auth/auth.service';
import { User } from '../../../core/models/user.model';

const FACILITY_ROLES = [
  { value: 'FacilityAdmin', label: 'Facility Admin' },
  { value: 'FacilityEditor', label: 'Facility Editor' },
  { value: 'FacilityViewer', label: 'Facility Viewer' },
];

@NgComponent({
  selector: 'app-create-facility-user-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>Create User</h2>
    <mat-dialog-content>
      <form [formGroup]="form" (ngSubmit)="confirm()">
        <mat-form-field appearance="outline" style="width:100%; margin-top:8px">
          <mat-label>Email</mat-label>
          <input matInput type="email" formControlName="email">
          @if (form.get('email')?.hasError('required') && form.get('email')?.touched) {
            <mat-error>Email is required</mat-error>
          }
          @if (form.get('email')?.hasError('email') && form.get('email')?.touched) {
            <mat-error>Invalid email</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline" style="width:100%">
          <mat-label>Role</mat-label>
          <mat-select formControlName="role">
            @for (r of roles; track r.value) {
              <mat-option [value]="r.value">{{ r.label }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="confirm()">Create</button>
    </mat-dialog-actions>
  `
})
export class CreateFacilityUserDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<CreateFacilityUserDialogComponent>);
  private readonly fb = ngInject(FormBuilder);

  readonly roles = FACILITY_ROLES;

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: ['FacilityViewer', Validators.required],
  });

  confirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.getRawValue());
    }
  }
}

@NgComponent({
  selector: 'app-setup-token-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>User Created</h2>
    <mat-dialog-content>
      <p>Share this setup link with the user:</p>
      <p style="word-break:break-all; background:#f5f5f5; padding:8px; border-radius:4px; font-family:monospace; font-size:13px;">
        {{ setupLink }}
      </p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="copyLink()">
        <mat-icon>content_copy</mat-icon> Copy Link
      </button>
      <button mat-flat-button color="primary" mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `
})
export class SetupTokenDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<SetupTokenDialogComponent>);
  private readonly snackBar = ngInject(MatSnackBar);

  setupLink = '';

  copyLink(): void {
    navigator.clipboard.writeText(this.setupLink).then(() => {
      this.snackBar.open('Link copied to clipboard', 'Close', { duration: 2000 });
    });
  }
}

@Component({
  selector: 'app-facility-users',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule,
    MatCardModule,
    MatTooltipModule,
    MatSelectModule,
    MatFormFieldModule,
  ],
  styles: [`
    .header-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }
    .spinner-wrap {
      display: flex;
      justify-content: center;
      padding: 32px;
    }
    table {
      width: 100%;
    }
    .approved-chip {
      background: #e8f5e9;
      color: #2e7d32;
    }
    .pending-chip {
      background: #fff3e0;
      color: #e65100;
    }
    .role-select {
      width: 160px;
    }
  `],
  template: `
    <div class="header-row">
      <h2>Facility Users</h2>
      <button mat-flat-button color="primary" (click)="openCreateDialog()">
        <mat-icon>person_add</mat-icon> Create User
      </button>
    </div>

    @if (isLoading()) {
      <div class="spinner-wrap">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    } @else {
      <mat-card>
        <mat-card-content>
          <table mat-table [dataSource]="users()">
            <ng-container matColumnDef="email">
              <th mat-header-cell *matHeaderCellDef>Email</th>
              <td mat-cell *matCellDef="let user">{{ user.email }}</td>
            </ng-container>

            <ng-container matColumnDef="role">
              <th mat-header-cell *matHeaderCellDef>Role</th>
              <td mat-cell *matCellDef="let user">
                <mat-select class="role-select" [value]="user.role" (selectionChange)="changeRole(user, $event.value)">
                  @for (r of facilityRoles; track r.value) {
                    <mat-option [value]="r.value">{{ r.label }}</mat-option>
                  }
                </mat-select>
              </td>
            </ng-container>

            <ng-container matColumnDef="isApproved">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let user">
                @if (user.isApproved) {
                  <mat-chip class="approved-chip">Approved</mat-chip>
                } @else {
                  <mat-chip class="pending-chip">Pending</mat-chip>
                }
              </td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let user">
                <button mat-icon-button color="warn" matTooltip="Remove" (click)="removeUser(user)">
                  <mat-icon>person_remove</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
          @if (users().length === 0) {
            <p style="text-align:center; color:#666; padding:24px;">No users in this facility.</p>
          }
        </mat-card-content>
      </mat-card>
    }
  `
})
export class FacilityUsersComponent implements OnInit {
  private readonly facilityService = inject(FacilityService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly users = signal<User[]>([]);
  readonly isLoading = signal(true);
  readonly facilityRoles = FACILITY_ROLES;

  readonly displayedColumns = ['email', 'role', 'isApproved', 'actions'];

  ngOnInit(): void {
    this.loadUsers();
  }

  private get facilityId(): string {
    return this.auth.facilityId() ?? '';
  }

  private loadUsers(): void {
    const fid = this.facilityId;
    if (!fid) {
      this.isLoading.set(false);
      return;
    }
    this.isLoading.set(true);
    this.facilityService.getUsers(fid).subscribe({
      next: users => {
        this.users.set(users);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load users', 'Close', { duration: 4000 });
      }
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CreateFacilityUserDialogComponent, { width: '360px' });

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.facilityService.createUser(this.facilityId, result.email, result.role).subscribe({
        next: response => {
          this.loadUsers();
          const setupLink = `${window.location.origin}/auth/set-password?email=${encodeURIComponent(result.email)}&token=${encodeURIComponent(response.setupToken)}`;
          const tokenDialog = this.dialog.open(SetupTokenDialogComponent, { width: '480px' });
          tokenDialog.componentInstance.setupLink = setupLink;
        },
        error: () => this.snackBar.open('Failed to create user', 'Close', { duration: 4000 })
      });
    });
  }

  changeRole(user: User, role: string): void {
    this.facilityService.updateUserRole(this.facilityId, user.id, role).subscribe({
      next: () => {
        this.snackBar.open('Role updated', 'Close', { duration: 3000 });
        this.users.update(list => list.map(u => u.id === user.id ? { ...u, role } : u));
      },
      error: () => {
        this.snackBar.open('Failed to update role', 'Close', { duration: 4000 });
        this.loadUsers();
      }
    });
  }

  removeUser(user: User): void {
    if (!confirm(`Remove ${user.email} from this facility?`)) return;
    this.facilityService.removeUser(this.facilityId, user.id).subscribe({
      next: () => {
        this.snackBar.open('User removed', 'Close', { duration: 3000 });
        this.users.update(list => list.filter(u => u.id !== user.id));
      },
      error: () => this.snackBar.open('Failed to remove user', 'Close', { duration: 4000 })
    });
  }
}
