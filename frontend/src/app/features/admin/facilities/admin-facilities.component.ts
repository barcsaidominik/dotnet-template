import type { OnInit } from '@angular/core';
import { Component, computed, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
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
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { from } from 'rxjs';
import { AdminService } from '../../../generated/client/services/admin.service';
import type { Facility } from '../../../core/models/facility.model';
import type { User } from '../../../core/models/user.model';
import { downloadBlobFile } from '../../../shared/utils/file-download.util';

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
  styleUrls: ['./facility-create-dialog.component.scss'],
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
  styleUrls: ['./facility-update-dialog.component.scss'],
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

type FacilityTableRow =
  | { kind: 'facility'; facility: Facility }
  | { kind: 'detail'; facility: Facility };

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
    MatFormFieldModule,
    MatInputModule,
    TranslateModule,
  ],
  templateUrl: './admin-facilities.component.html',
  styleUrls: ['./admin-facilities.component.scss'],
})
export class AdminFacilitiesPageComponent implements OnInit {
  private readonly adminApi = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  readonly facilities = signal<Facility[]>([]);
  readonly allUsers = signal<User[]>([]);
  readonly expandedFacilityId = signal<string | null>(null);
  readonly isLoading = signal(true);
  readonly isExporting = signal(false);
  readonly searchTerm = signal('');
  readonly sortBy = signal<string | null>(null);
  readonly sortDescending = signal(false);

  readonly usersByFacility = computed(() => {
    const map = new Map<string, User[]>();
    for (const user of this.allUsers()) {
      if (user.facilityId) {
        const list = map.get(user.facilityId) ?? [];
        list.push(user);
        map.set(user.facilityId, list);
      }
    }
    return map;
  });

  readonly displayedColumns = ['name', 'employeeCount', 'actions'];
  readonly tableRows = computed<FacilityTableRow[]>(() =>
    this.facilities().flatMap((facility) =>
      this.expandedFacilityId() === facility.id
        ? [
            { kind: 'facility', facility } satisfies FacilityTableRow,
            { kind: 'detail', facility } satisfies FacilityTableRow,
          ]
        : [{ kind: 'facility', facility } satisfies FacilityTableRow]
    )
  );

  ngOnInit(): void {
    this.loadFacilities();
  }

  private loadFacilities(): void {
    this.isLoading.set(true);
    const facilitiesParams = this.buildFacilitiesQueryParams();
    from(this.adminApi.apiAdminFacilitiesGet$Json(facilitiesParams)).subscribe({
      next: (facilities) => {
        this.facilities.set(facilities);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(
          this.translate.instant('admin.facilities.failedToLoad'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
    from(this.adminApi.apiAdminUsersGet$Json()).subscribe({
      next: (users) => this.allUsers.set(users),
    });
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value.trim());
    this.loadFacilities();
  }

  clearSearch(): void {
    if (!this.searchTerm()) {
      return;
    }

    this.searchTerm.set('');
    this.loadFacilities();
  }

  toggleSort(column: 'name'): void {
    if (this.sortBy() !== column) {
      this.sortBy.set(column);
      this.sortDescending.set(false);
    } else if (!this.sortDescending()) {
      this.sortDescending.set(true);
    } else {
      this.sortBy.set(null);
      this.sortDescending.set(false);
    }

    this.loadFacilities();
  }

  sortIcon(column: string): string {
    if (this.sortBy() !== column) {
      return 'unfold_more';
    }

    return this.sortDescending() ? 'south' : 'north';
  }

  readonly isFacilityRow = (_index: number, row: FacilityTableRow): boolean => row.kind === 'facility';
  readonly isDetailRow = (_index: number, row: FacilityTableRow): boolean => row.kind === 'detail';

  toggleExpand(facility: Facility): void {
    const current = this.expandedFacilityId();
    this.expandedFacilityId.set(current === facility.id ? null : facility.id);
  }

  getEmployeeCount(facilityId: string): number {
    return this.usersByFacility().get(facilityId)?.length ?? 0;
  }

  getUsersForFacility(facilityId: string): User[] {
    return this.usersByFacility().get(facilityId) ?? [];
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(FacilityCreateDialogComponent, { width: '360px' });

    ref.afterClosed().subscribe((name) => {
      if (!name) {
        return;
      }
      from(this.adminApi.apiAdminFacilitiesPost$Json({ body: { name } })).subscribe({
        next: () => {
          this.snackBar.open(
            this.translate.instant('admin.facilities.facilityCreated'),
            this.translate.instant('common.close'),
            { duration: 3000 }
          );
          this.loadFacilities();
        },
        error: () =>
          this.snackBar.open(
            this.translate.instant('admin.facilities.failedToCreate'),
            this.translate.instant('common.close'),
            { duration: 4000 }
          ),
      });
    });
  }

  openEditDialog(facility: Facility): void {
    const ref = this.dialog.open(FacilityUpdateDialogComponent, {
      width: '360px',
      data: { name: facility.name },
    });

    ref.afterClosed().subscribe((name) => {
      if (!name) {
        return;
      }
      from(
        this.adminApi.apiAdminFacilitiesFacilityIdPut({
          facilityId: facility.id,
          body: { name },
        })
      ).subscribe({
        next: () => {
          this.snackBar.open(
            this.translate.instant('admin.facilities.facilityUpdated'),
            this.translate.instant('common.close'),
            { duration: 3000 }
          );
          this.facilities.update((list) =>
            list.map((f) => (f.id === facility.id ? { ...f, name } : f))
          );
        },
        error: () =>
          this.snackBar.open(
            this.translate.instant('admin.facilities.failedToUpdate'),
            this.translate.instant('common.close'),
            { duration: 4000 }
          ),
      });
    });
  }

  deleteFacility(facility: Facility): void {
    if (
      !confirm(this.translate.instant('admin.facilities.deleteConfirm', { name: facility.name }))
    ) {
      return;
    }
    from(this.adminApi.apiAdminFacilitiesFacilityIdDelete({ facilityId: facility.id })).subscribe({
      next: () => {
        this.snackBar.open(
          this.translate.instant('admin.facilities.facilityDeleted'),
          this.translate.instant('common.close'),
          { duration: 3000 }
        );
        this.facilities.update((list) => list.filter((f) => f.id !== facility.id));
      },
      error: (err) =>
        this.snackBar.open(
          err?.error?.detail ?? this.translate.instant('admin.facilities.failedToDelete'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        ),
    });
  }

  exportFacilities(): void {
    this.isExporting.set(true);
    from(this.adminApi.apiAdminFacilitiesExportGet$Response()).subscribe({
      next: (response) => {
        downloadBlobFile(response.body as Blob, response.headers, 'facilities.xlsx');
        this.isExporting.set(false);
      },
      error: () => {
        this.isExporting.set(false);
        this.snackBar.open(
          this.translate.instant('admin.facilities.failedToExport'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  private buildFacilitiesQueryParams(): { search?: string; sortBy?: string; sortDescending?: boolean } {
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
