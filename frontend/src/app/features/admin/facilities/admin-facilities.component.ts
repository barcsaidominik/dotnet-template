import { Component, inject, signal, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { from } from 'rxjs';
import { AdminService } from '../../../generated/client/services/admin.service';
import { Facility } from '../../../core/models/facility.model';

@NgComponent({
  selector: 'app-facility-create-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    TranslateModule,
  ],
  templateUrl: './facility-create-dialog.component.html',
  styleUrls: ['./facility-create-dialog.component.scss']
})
export class FacilityCreateDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<FacilityCreateDialogComponent>);
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

@NgComponent({
  selector: 'app-facility-update-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    TranslateModule,
  ],
  templateUrl: './facility-update-dialog.component.html',
  styleUrls: ['./facility-update-dialog.component.scss']
})
export class FacilityUpdateDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<FacilityUpdateDialogComponent>);
  readonly data = ngInject<{ name: string }>(MAT_DIALOG_DATA);
  private readonly fb = ngInject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    name: [this.data.name, Validators.required],
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
    TranslateModule,
  ],
  templateUrl: './admin-facilities.component.html',
  styleUrls: ['./admin-facilities.component.scss']
})
export class AdminFacilitiesPageComponent implements OnInit {
  private readonly adminApi = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  readonly facilities = signal<Facility[]>([]);
  readonly isLoading = signal(true);

  readonly displayedColumns = ['name', 'id', 'actions'];

  ngOnInit(): void {
    this.loadFacilities();
  }

  private loadFacilities(): void {
    this.isLoading.set(true);
    from(this.adminApi.apiAdminFacilitiesGet$Json()).subscribe({
      next: facilities => {
        this.facilities.set(facilities);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(this.translate.instant('admin.facilities.failedToLoad'), this.translate.instant('common.close'), { duration: 4000 });
      }
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(FacilityCreateDialogComponent, { width: '360px' });

    ref.afterClosed().subscribe(name => {
      if (!name) return;
      from(this.adminApi.apiAdminFacilitiesPost$Json({ body: { name } })).subscribe({
        next: () => {
          this.snackBar.open(this.translate.instant('admin.facilities.facilityCreated'), this.translate.instant('common.close'), { duration: 3000 });
          this.loadFacilities();
        },
        error: () => this.snackBar.open(this.translate.instant('admin.facilities.failedToCreate'), this.translate.instant('common.close'), { duration: 4000 })
      });
    });
  }

  openEditDialog(facility: Facility): void {
    const ref = this.dialog.open(FacilityUpdateDialogComponent, {
      width: '360px',
      data: { name: facility.name }
    });

    ref.afterClosed().subscribe(name => {
      if (!name) return;
      from(this.adminApi.apiAdminFacilitiesFacilityIdPut({
        facilityId: facility.id,
        body: { name }
      })).subscribe({
        next: () => {
          this.snackBar.open(this.translate.instant('admin.facilities.facilityUpdated'), this.translate.instant('common.close'), { duration: 3000 });
          this.facilities.update(list => list.map(f => f.id === facility.id ? { ...f, name } : f));
        },
        error: () => this.snackBar.open(this.translate.instant('admin.facilities.failedToUpdate'), this.translate.instant('common.close'), { duration: 4000 })
      });
    });
  }

  deleteFacility(facility: Facility): void {
    if (!confirm(this.translate.instant('admin.facilities.deleteConfirm', { name: facility.name }))) return;
    from(this.adminApi.apiAdminFacilitiesFacilityIdDelete({ facilityId: facility.id })).subscribe({
      next: () => {
        this.snackBar.open(this.translate.instant('admin.facilities.facilityDeleted'), this.translate.instant('common.close'), { duration: 3000 });
        this.facilities.update(list => list.filter(f => f.id !== facility.id));
      },
      error: () => this.snackBar.open(this.translate.instant('admin.facilities.failedToDelete'), this.translate.instant('common.close'), { duration: 4000 })
    });
  }
}
