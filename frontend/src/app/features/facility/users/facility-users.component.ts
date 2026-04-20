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
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { from } from 'rxjs';
import { FacilityUsersService } from '../../../generated/client/services/facility-users.service';
import { AuthService } from '../../../core/auth/auth.service';
import type { User } from '../../../core/models/user.model';

const FACILITY_ROLES = [
  { value: 'FacilityAdmin', label: 'Facility Admin' },
  { value: 'FacilityEditor', label: 'Facility Editor' },
  { value: 'FacilityViewer', label: 'Facility Viewer' },
];

@NgComponent({
  selector: 'app-facility-user-create-dialog',
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
  templateUrl: './facility-user-create-dialog.component.html',
  styleUrls: ['./facility-user-create-dialog.component.scss'],
})
export class FacilityUserCreateDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<FacilityUserCreateDialogComponent>);
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
  selector: 'app-token-setup-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, TranslateModule],
  templateUrl: './token-setup-dialog.component.html',
  styleUrls: ['./token-setup-dialog.component.scss'],
})
export class TokenSetupDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<TokenSetupDialogComponent>);

  setupLink = '';
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
    TranslateModule,
  ],
  templateUrl: './facility-users.component.html',
  styleUrls: ['./facility-users.component.scss'],
})
export class FacilityUsersPageComponent implements OnInit {
  private readonly facilityUsersApi = inject(FacilityUsersService);
  readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);

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
    from(this.facilityUsersApi.apiFacilitiesFacilityIdUsersGet$Json({ facilityId: fid })).subscribe(
      {
        next: (users) => {
          this.users.set(users);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
          this.snackBar.open(
            this.translate.instant('facility.users.failedToLoad'),
            this.translate.instant('common.close'),
            { duration: 4000 }
          );
        },
      }
    );
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(FacilityUserCreateDialogComponent, { width: '360px' });

    ref
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (!result) {
          return;
        }
        from(
          this.facilityUsersApi.apiFacilitiesFacilityIdUsersPost$Json({
            facilityId: this.facilityId,
            body: {
              email: result.email,
              role: result.role,
            },
          })
        ).subscribe({
          next: (response) => {
            this.loadUsers();
            const setupLink = `${window.location.origin}/auth/set-password?email=${encodeURIComponent(result.email)}&token=${encodeURIComponent(response.setupToken)}`;
            const tokenDialog = this.dialog.open(TokenSetupDialogComponent, { width: '480px' });
            tokenDialog.componentInstance.setupLink = setupLink;
          },
          error: () =>
            this.snackBar.open(
              this.translate.instant('facility.users.failedToCreate'),
              this.translate.instant('common.close'),
              { duration: 4000 }
            ),
        });
      });
  }

  changeRole(user: User, role: string): void {
    from(
      this.facilityUsersApi.apiFacilitiesFacilityIdUsersUserIdRolePut({
        facilityId: this.facilityId,
        userId: user.id,
        body: { role },
      })
    ).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('facility.users.roleUpdated'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.users.update((list) => list.map((u) => (u.id === user.id ? { ...u, role } : u)));
      },
      error: () => {
        this.snackBar.open(
          this.translate.instant('facility.users.failedToUpdateRole'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
        this.loadUsers();
      },
    });
  }

  removeUser(user: User): void {
    if (!confirm(this.translate.instant('facility.users.removeConfirm', { email: user.email }))) {
      return;
    }
    from(
      this.facilityUsersApi.apiFacilitiesFacilityIdUsersUserIdDelete({
        facilityId: this.facilityId,
        userId: user.id,
      })
    ).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('facility.users.userRemoved'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.users.update((list) => list.filter((u) => u.id !== user.id));
      },
      error: () =>
        this.snackBar.open(
          this.translate.instant('facility.users.failedToRemove'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        ),
    });
  }
}
