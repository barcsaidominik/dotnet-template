import type { OnInit } from '@angular/core';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import {
  MatDialog,
  MatDialogModule,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { from } from 'rxjs';
import { AdminService } from '../../../generated/client/services/admin.service';
import { FacilityUsersService } from '../../../generated/client/services/facility-users.service';
import { AuthService } from '../../../core/auth/auth.service';
import type { User } from '../../../core/models/user.model';
import type { Facility } from '../../../core/models/facility.model';
import { downloadBlobFile } from '../../../shared/utils/file-download.util';

@NgComponent({
  selector: 'app-user-approve-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    TranslateModule,
  ],
  templateUrl: './user-approve-dialog.component.html',
  styleUrls: ['./user-approve-dialog.component.scss'],
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
    TranslateModule,
  ],
  templateUrl: './create-facility-user-dialog.component.html',
  styleUrls: ['./create-facility-user-dialog.component.scss'],
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
  imports: [MatDialogModule, MatButtonModule, TranslateModule],
  templateUrl: './token-setup-dialog.component.html',
  styleUrls: ['./token-setup-dialog.component.scss'],
})
export class AdminTokenSetupDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<AdminTokenSetupDialogComponent>);

  setupLink = '';
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
    MatFormFieldModule,
    MatInputModule,
    TranslateModule,
  ],
  templateUrl: './admin-users.component.html',
  styleUrls: ['./admin-users.component.scss'],
})
export class AdminUsersPageComponent implements OnInit {
  private readonly adminApi = inject(AdminService);
  private readonly facilityUsersApi = inject(FacilityUsersService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);
  readonly auth = inject(AuthService);

  readonly users = signal<User[]>([]);
  readonly facilities = signal<Facility[]>([]);
  readonly isLoading = signal(true);
  readonly isExporting = signal(false);
  readonly searchTerm = signal('');
  readonly sortBy = signal<string | null>(null);
  readonly sortDescending = signal(false);

  readonly displayedColumns = ['email', 'role', 'isApproved', 'facilityName', 'actions'];

  ngOnInit(): void {
    this.loadData();
  }

  getFacilityName(facilityId: string | null): string {
    if (!facilityId) {
      return '-';
    }
    return this.facilities().find((f) => f.id === facilityId)?.name ?? '-';
  }

  private loadData(): void {
    this.isLoading.set(true);
    from(this.adminApi.apiAdminUsersGet$Json(this.buildUsersQueryParams())).subscribe({
      next: (users) => {
        this.users.set(users);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(
          this.translate.instant('admin.users.failedToLoad'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
    from(this.adminApi.apiAdminFacilitiesGet$Json()).subscribe({
      next: (facilities) => this.facilities.set(facilities),
    });
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value.trim());
    this.loadData();
  }

  clearSearch(): void {
    if (!this.searchTerm()) {
      return;
    }

    this.searchTerm.set('');
    this.loadData();
  }

  toggleSort(column: 'email' | 'role'): void {
    if (this.sortBy() !== column) {
      this.sortBy.set(column);
      this.sortDescending.set(false);
    } else if (!this.sortDescending()) {
      this.sortDescending.set(true);
    } else {
      this.sortBy.set(null);
      this.sortDescending.set(false);
    }

    this.loadData();
  }

  sortIcon(column: string): string {
    if (this.sortBy() !== column) {
      return 'unfold_more';
    }

    return this.sortDescending() ? 'south' : 'north';
  }

  openApproveDialog(user: User): void {
    const ref = this.dialog.open(UserApproveDialogComponent, {
      width: '360px',
      data: { facilities: this.facilities() },
    });

    ref
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) {
          return;
        }
        from(
          this.adminApi.apiAdminUsersUserIdApprovePost({
            userId: user.id,
            body: {
              facilityId: result.facilityId,
              role: result.role,
            },
          })
        ).subscribe({
          next: () => {
            this.snackBar.open(
              this.translate.instant('admin.users.userApproved'),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
            this.loadData();
          },
          error: () =>
            this.snackBar.open(
              this.translate.instant('admin.users.failedToApprove'),
              this.translate.instant('common.close'),
              { duration: 4000 }
            ),
        });
      });
  }

  openCreateUserDialog(): void {
    const ref = this.dialog.open(CreateFacilityUserDialogComponent, {
      width: '400px',
      data: { facilities: this.facilities() },
    });

    ref
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) {
          return;
        }
        from(
          this.facilityUsersApi.apiFacilitiesFacilityIdUsersPost$Json({
            facilityId: result.facilityId,
            body: {
              email: result.email,
              role: result.role,
            },
          })
        ).subscribe({
          next: (response) => {
            this.loadData();
            const setupLink = `${window.location.origin}/auth/set-password?email=${encodeURIComponent(result.email)}&token=${encodeURIComponent(response.setupToken)}`;
            const tokenDialog = this.dialog.open(AdminTokenSetupDialogComponent, { width: '480px' });
            tokenDialog.componentInstance.setupLink = setupLink;
          },
          error: () =>
            this.snackBar.open(
              this.translate.instant('admin.users.failedToCreate'),
              this.translate.instant('common.close'),
              { duration: 4000 }
            ),
        });
      });
  }

  exportUsers(): void {
    this.isExporting.set(true);
    from(this.adminApi.apiAdminUsersExportGet$Response(this.buildUsersQueryParams())).subscribe({
      next: (response) => {
        downloadBlobFile(response.body as Blob, response.headers, 'users.xlsx');
        this.isExporting.set(false);
      },
      error: () => {
        this.isExporting.set(false);
        this.snackBar.open(
          this.translate.instant('admin.users.failedToExport'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  deleteUser(user: User): void {
    if (!confirm(this.translate.instant('admin.users.deleteConfirm', { email: user.email }))) {
      return;
    }
    from(this.adminApi.apiAdminUsersUserIdDelete({ userId: user.id })).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('admin.users.userDeleted'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.users.update((list) => list.filter((u) => u.id !== user.id));
      },
      error: (err) => {
        console.error('Failed to delete user:', err);
        this.snackBar.open(
          this.translate.instant('errors.Error.Unexpected'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  private buildUsersQueryParams(): { search?: string; sortBy?: string; sortDescending?: boolean } {
    const params: { search?: string; sortBy?: string; sortDescending?: boolean } = {};

    if (this.searchTerm()) {
      params.search = this.searchTerm();
    }

    if (this.sortBy()) {
      params.sortBy = this.sortBy() ?? undefined;
      params.sortDescending = this.sortDescending();
    }

    return params;
  }
}
