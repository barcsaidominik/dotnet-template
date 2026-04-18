import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { Facility } from '../../../core/models/facility.model';

@NgComponent({
  selector: 'app-create-facility-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>Create Facility</h2>
    <mat-dialog-content>
      <form [formGroup]="form" (ngSubmit)="confirm()">
        <mat-form-field appearance="outline" style="width:100%; margin-top:8px">
          <mat-label>Facility Name</mat-label>
          <input matInput formControlName="name">
          @if (form.get('name')?.hasError('required') && form.get('name')?.touched) {
            <mat-error>Name is required</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="confirm()">Create</button>
    </mat-dialog-actions>
  `
})
export class CreateFacilityDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<CreateFacilityDialogComponent>);
  private readonly fb = ngInject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
  });

  confirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.getRawValue().name);
    }
  }
}

@Component({
  selector: 'app-admin-facilities',
  standalone: true,
  imports: [
    MatTableModule,
    MatButtonModule,
    MatIconModule,
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
  `],
  template: `
    <div class="header-row">
      <h2>Facilities</h2>
      <button mat-flat-button color="primary" (click)="openCreateDialog()">
        <mat-icon>add</mat-icon> Create Facility
      </button>
    </div>

    @if (isLoading()) {
      <div class="spinner-wrap">
        <mat-spinner diameter="48"></mat-spinner>
      </div>
    } @else {
      <mat-card>
        <mat-card-content>
          <table mat-table [dataSource]="facilities()">
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Name</th>
              <td mat-cell *matCellDef="let facility">{{ facility.name }}</td>
            </ng-container>

            <ng-container matColumnDef="id">
              <th mat-header-cell *matHeaderCellDef>ID</th>
              <td mat-cell *matCellDef="let facility">{{ facility.id }}</td>
            </ng-container>

            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let facility">
                <button mat-icon-button color="warn" matTooltip="Delete" (click)="deleteFacility(facility)">
                  <mat-icon>delete</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
          @if (facilities().length === 0) {
            <p style="text-align:center; color:#666; padding:24px;">No facilities found.</p>
          }
        </mat-card-content>
      </mat-card>
    }
  `
})
export class AdminFacilitiesComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly facilities = signal<Facility[]>([]);
  readonly isLoading = signal(true);

  readonly displayedColumns = ['name', 'id', 'actions'];

  ngOnInit(): void {
    this.loadFacilities();
  }

  private loadFacilities(): void {
    this.isLoading.set(true);
    this.adminService.getFacilities().subscribe({
      next: facilities => {
        this.facilities.set(facilities);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load facilities', 'Close', { duration: 4000 });
      }
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CreateFacilityDialogComponent, { width: '360px' });

    ref.afterClosed().subscribe(name => {
      if (!name) return;
      this.adminService.createFacility(name).subscribe({
        next: () => {
          this.snackBar.open('Facility created', 'Close', { duration: 3000 });
          this.loadFacilities();
        },
        error: () => this.snackBar.open('Failed to create facility', 'Close', { duration: 4000 })
      });
    });
  }

  deleteFacility(facility: Facility): void {
    if (!confirm(`Delete facility "${facility.name}"?`)) return;
    this.adminService.deleteFacility(facility.id).subscribe({
      next: () => {
        this.snackBar.open('Facility deleted', 'Close', { duration: 3000 });
        this.facilities.update(list => list.filter(f => f.id !== facility.id));
      },
      error: () => this.snackBar.open('Failed to delete facility', 'Close', { duration: 4000 })
    });
  }
}
