import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { from } from 'rxjs';
import { AdminService } from '../../../generated/client/services/admin.service';
import { FacilityUsersService } from '../../../generated/client/services/facility-users.service';
import { User } from '../../../core/models/user.model';
import { Facility } from '../../../core/models/facility.model';

@NgComponent({
  selector: 'app-user-approve-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
  ],
  templateUrl: './user-approve-dialog.component.html',
  styleUrls: ['./user-approve-dialog.component.scss']
})
export class UserApproveDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<UserApproveDialogComponent>);
  readonly data = ngInject<{ facilities: Facility[] }>(MAT_DIALOG_DATA);
  private readonly fb = ngInject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    facilityId: ['', Validators.required],
    role: ['', Validators.required],
  });

  confirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.getRawValue());
    }
  }
}

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
  templateUrl: './create-facility-user-dialog.component.html',
  styleUrls: ['./create-facility-user-dialog.component.scss']
})
export class CreateFacilityUserDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<CreateFacilityUserDialogComponent>);
  readonly data = ngInject<{ facilities: Facility[] }>(MAT_DIALOG_DATA);
  private readonly fb = ngInject(FormBuilder);

  readonly facilityRoles = [
    { value: 'FacilityAdmin', label: 'Facility Admin' },
    { value: 'FacilityEditor', label: 'Facility Editor' },
    { value: 'FacilityViewer', label: 'Facility Viewer' },
  ];

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    facilityId: ['', Validators.required],
    role: ['FacilityViewer', Validators.required],
  });

  confirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.getRawValue());
    }
  }
}

@NgComponent({
  selector: 'app-admin-token-setup-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  templateUrl: './token-setup-dialog.component.html',
  styleUrls: ['./token-setup-dialog.component.scss']
})
export class AdminTokenSetupDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<AdminTokenSetupDialogComponent>);
  private readonly snackBar = ngInject(MatSnackBar);

  setupLink = '';

  copyLink(): void {
    navigator.clipboard.writeText(this.setupLink).then(() => {
      this.snackBar.open('Link copied to clipboard', 'Close', { duration: 2000 });
    });
  }
}

@Component({
  selector: 'app-admin-users',
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
  ],
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.scss']
})
export class AdminUsersPageComponent implements OnInit {
  private readonly adminApi = inject(AdminService);
  private readonly facilityUsersApi = inject(FacilityUsersService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly users = signal<User[]>([]);
  readonly facilities = signal<Facility[]>([]);
  readonly isLoading = signal(true);

  readonly displayedColumns = ['email', 'role', 'isApproved', 'facilityId', 'actions'];

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.isLoading.set(true);
    from(this.adminApi.apiAdminUsersGet$Json()).subscribe({
      next: users => {
        this.users.set(users);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load users', 'Close', { duration: 4000 });
      }
    });
    from(this.adminApi.apiAdminFacilitiesGet$Json()).subscribe({
      next: facilities => this.facilities.set(facilities)
    });
  }

  openApproveDialog(user: User): void {
    const ref = this.dialog.open(UserApproveDialogComponent, {
      width: '360px',
      data: { facilities: this.facilities() }
    });

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      from(this.adminApi.apiAdminUsersUserIdApprovePost({
        userId: user.id,
        body: {
          facilityId: result.facilityId,
          role: result.role
        }
      })).subscribe({
        next: () => {
          this.snackBar.open('User approved', 'Close', { duration: 3000 });
          this.loadData();
        },
        error: () => this.snackBar.open('Failed to approve user', 'Close', { duration: 4000 })
      });
    });
  }

  openCreateUserDialog(): void {
    const ref = this.dialog.open(CreateFacilityUserDialogComponent, {
      width: '400px',
      data: { facilities: this.facilities() }
    });

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      from(this.facilityUsersApi.apiFacilitiesFacilityIdUsersPost$Json({
        facilityId: result.facilityId,
        body: {
          email: result.email,
          role: result.role
        }
      })).subscribe({
        next: response => {
          this.loadData();
          const setupLink = `${window.location.origin}/auth/set-password?email=${encodeURIComponent(result.email)}&token=${encodeURIComponent(response.setupToken)}`;
          const tokenDialog = this.dialog.open(AdminTokenSetupDialogComponent, { width: '480px' });
          tokenDialog.componentInstance.setupLink = setupLink;
        },
        error: () => this.snackBar.open('Failed to create user', 'Close', { duration: 4000 })
      });
    });
  }

  deleteUser(user: User): void {
    if (!confirm(`Delete user ${user.email}?`)) return;
    from(this.adminApi.apiAdminUsersUserIdDelete({ userId: user.id })).subscribe({
      next: () => {
        this.snackBar.open('User deleted', 'Close', { duration: 3000 });
        this.users.update(list => list.filter(u => u.id !== user.id));
      },
      error: (err) => {
        const msg = err?.error?.detail ?? 'Failed to delete user';
        this.snackBar.open(msg, 'Close', { duration: 4000 });
      }
    });
  }
}
