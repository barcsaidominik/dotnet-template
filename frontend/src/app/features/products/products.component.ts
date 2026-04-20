import { Component, inject, signal } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import type { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule } from '@ngx-translate/core';
import { from } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { mapProductDto } from '../../shared/mappers/product.mapper';
import { ProductsBaseComponent } from '../../shared/products/products-base.component';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    DecimalPipe,
    DatePipe,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatDialogModule,
    MatCardModule,
    MatPaginatorModule,
    MatTooltipModule,
    TranslateModule,
  ],
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.scss'],
})
export class ProductsPageComponent extends ProductsBaseComponent {
  protected override readonly i18nPrefix = 'products';
  protected override readonly routePath = '/products';

  readonly auth = inject(AuthService);
  readonly totalCount = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(20);

  override loadProducts(): void {
    this.isLoading.set(true);
    from(
      this.productsApi.apiProductsGet$Json({
        page: this.page() + 1,
        pageSize: this.pageSize(),
      })
    ).subscribe({
      next: (result) => {
        this.totalCount.set(Number(result.totalCount ?? 0));
        this.products.set((result.items ?? []).map(mapProductDto));
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(
          this.translate.instant(`${this.i18nPrefix}.failedToLoad`),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadProducts();
  }
}
