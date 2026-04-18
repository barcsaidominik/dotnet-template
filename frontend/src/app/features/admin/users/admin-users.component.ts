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
import { AdminService } from '../../../core/services/admin.service';
import { User } from '../../../core/models/user.model';
import { Facility } from '../../../core/models/facility.model';

// Approve dialog component
@NgComponent({
  selector: 'app-approve-user-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>Approve User</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" style="width:100%; margin-top:8px">
          <mat-label>Facility</mat-label>
          <mat-select formControlName="facilityId">
            @for (f of data.facilities; track f.id) {
              <mat-option [value]="f.id">{{ f.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" style="width:100%">
          <mat-label>Role</mat-label>
          <mat-select formControlName="role">
            <mat-option value="FacilityAdmin">Facility Admin</mat-option>
            <mat-option value="FacilityEditor">Facility Editor</mat-option>
            <mat-option value="FacilityViewer">Facility Viewer</mat-option>
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="confirm()">Approve</button>
    </mat-dialog-actions>
  `
})
export class ApproveUserDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<ApproveUserDialogComponent>);
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
  `],
  template: `
    <div class="header-row">
      <h2>Users</h2>
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
              <td mat-cell *matCellDef="let user">{{ user.role ?? '-' }}</td>
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

            <ng-container matColumnDef="facilityId">
              <th mat-header-cell *matHeaderCellDef>Facility ID</th>
              <td mat-cell *matCellDef="let user">{{ user.facilityId ?? '-' }}</td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let user">
                @if (!user.isApproved) {
                  <button mat-icon-button color="primary" matTooltip="Approve" (click)="openApproveDialog(user)">
                    <mat-icon>check_circle</mat-icon>
                  </button>
                }
                <button mat-icon-button color="warn" matTooltip="Delete" (click)="deleteUser(user)">
                  <mat-icon>delete</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
          @if (users().length === 0) {
            <p style="text-align:center; color:#666; padding:24px;">No users found.</p>
          }
        </mat-card-content>
      </mat-card>
    }
  `
})
export class AdminUsersComponent implements OnInit {
  private readonly adminService = inject(AdminService);
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
    this.adminService.getUsers().subscribe({
      next: users => {
        this.users.set(users);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load users', 'Close', { duration: 4000 });
      }
    });
    this.adminService.getFacilities().subscribe({
      next: facilities => this.facilities.set(facilities)
    });
  }

  openApproveDialog(user: User): void {
    const ref = this.dialog.open(ApproveUserDialogComponent, {
      width: '360px',
      data: { facilities: this.facilities() }
    });

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.adminService.approveUser(user.id, result.facilityId, result.role).subscribe({
        next: () => {
          this.snackBar.open('User approved', 'Close', { duration: 3000 });
          this.loadData();
        },
        error: () => this.snackBar.open('Failed to approve user', 'Close', { duration: 4000 })
      });
    });
  }

  deleteUser(user: User): void {
    if (!confirm(`Delete user ${user.email}?`)) return;
    this.adminService.deleteUser(user.id).subscribe({
      next: () => {
        this.snackBar.open('User deleted', 'Close', { duration: 3000 });
        this.users.update(list => list.filter(u => u.id !== user.id));
      },
      error: () => this.snackBar.open('Failed to delete user', 'Close', { duration: 4000 })
    });
  }
}
