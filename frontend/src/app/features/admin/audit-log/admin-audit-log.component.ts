import type { OnInit } from '@angular/core';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe, LowerCasePipe, SlicePipe } from '@angular/common';
import { from } from 'rxjs';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule } from '@angular/material/paginator';
import type { PageEvent } from '@angular/material/paginator';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AdminService } from '../../../generated/client/services/admin.service';
import type { ApiAdminAuditGet$Json$Params } from '../../../generated/client/fn/admin/api-admin-audit-get-json';
import type { AuditEntryDto } from '../../../generated/client/models/audit-entry-dto';

@Component({
  selector: 'app-admin-audit-log',
  standalone: true,
  imports: [
    DatePipe,
    LowerCasePipe,
    SlicePipe,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule,
    MatTooltipModule,
    MatPaginatorModule,
    TranslatePipe,
  ],
  templateUrl: './admin-audit-log.component.html',
  styleUrls: ['./admin-audit-log.component.scss'],
})
export class AdminAuditLogPageComponent implements OnInit {
  private readonly adminApi = inject(AdminService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  readonly destroyRef = inject(DestroyRef);

  readonly entries = signal<AuditEntryDto[]>([]);
  readonly isLoading = signal(true);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(50);

  readonly entityTypeFilter = signal('');
  readonly actionFilter = signal('');
  readonly fromFilter = signal('');
  readonly toFilter = signal('');

  readonly displayedColumns = [
    'occurredAt',
    'entityType',
    'entityId',
    'action',
    'userEmail',
    'changes',
  ];
  readonly entityTypes = ['Facility', 'Product'];
  readonly actions = ['Created', 'Updated', 'Deleted'];

  ngOnInit(): void {
    this.loadData();
  }

  onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadData();
  }

  applyFilters(): void {
    this.page.set(1);
    this.loadData();
  }

  clearFilters(): void {
    this.entityTypeFilter.set('');
    this.actionFilter.set('');
    this.fromFilter.set('');
    this.toFilter.set('');
    this.page.set(1);
    this.loadData();
  }

  truncateChanges(value: string | null | undefined): string {
    if (!value) {
      return '-';
    }

    const maxLength = 100;
    return value.length > maxLength ? value.substring(0, maxLength) + '...' : value;
  }

  private loadData(): void {
    this.isLoading.set(true);
    from(this.adminApi.apiAdminAuditGet$Json(this.buildParams())).subscribe({
      next: (result) => {
        this.entries.set(result.items ?? []);
        const count = result.totalCount;
        this.totalCount.set(typeof count === 'number' ? count : parseInt(count as string, 10));
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(
          this.translate.instant('admin.auditLog.failedToLoad'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  private buildParams(): ApiAdminAuditGet$Json$Params {
    const params: ApiAdminAuditGet$Json$Params = {
      page: this.page(),
      pageSize: this.pageSize(),
      sortDescending: true,
    };

    if (this.entityTypeFilter()) {
      params.entityType = this.entityTypeFilter();
    }

    if (this.actionFilter()) {
      params.action = this.actionFilter();
    }

    if (this.fromFilter()) {
      params.from = new Date(this.fromFilter()).toISOString();
    }

    if (this.toFilter()) {
      const to = new Date(this.toFilter());
      to.setHours(23, 59, 59, 999);
      params.to = to.toISOString();
    }

    return params;
  }
}
