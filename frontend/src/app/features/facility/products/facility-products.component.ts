import { Component, signal } from '@angular/core';
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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';
import { from } from 'rxjs';
import type { Product } from '../../../core/models/product.model';
import { mapProductDto } from '../../../shared/mappers/product.mapper';
import { ProductDialogComponent } from '../../../shared/products/product-dialog.component';
import type { ProductDialogData } from '../../../shared/products/product-dialog.component';
import { ProductsBaseComponent } from '../../../shared/products/products-base.component';

@Component({
  selector: 'app-facility-products',
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
    MatFormFieldModule,
    MatInputModule,
    TranslatePipe,
  ],
  templateUrl: './facility-products.component.html',
  styleUrls: ['./facility-products.component.scss'],
})
export class FacilityProductsPageComponent extends ProductsBaseComponent {
  protected override readonly i18nPrefix = 'facility.products';
  protected override readonly routePath = '/facility/products';
  protected override readonly exportFilename = 'facility-products.xlsx';

  readonly totalCount = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(20);

  override loadProducts(): void {
    this.isLoading.set(true);
    from(
      this.productsApi.apiProductsGet$Json(
        this.buildProductsQueryParams(this.page() + 1, this.pageSize())
      )
    ).subscribe({
      next: (result) => {
        this.products.set((result.items ?? []).map(mapProductDto));
        this.totalCount.set(Number(result.totalCount ?? 0));
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

  openEditDialog(product: Product): void {
    const ref = this.dialog.open(ProductDialogComponent, {
      width: '360px',
      data: {
        mode: 'update',
        prefix: this.i18nPrefix,
        name: product.name,
        price: product.price,
        quantity: product.quantity,
      } as ProductDialogData,
    });

    ref
      .afterClosed()
      .subscribe((result: { name: string; price: number; quantity: number } | undefined) => {
        if (!result) {
          return;
        }
        from(
          this.productsApi.apiProductsIdPut({
            id: product.id,
            body: {
              name: result.name,
              price: result.price,
              quantity: result.quantity,
              rowVersion: product.rowVersion,
            },
          })
        ).subscribe({
          next: () => {
            this.loadProducts();
            this.snackBar.open(
              this.translate.instant(`${this.i18nPrefix}.productUpdated`),
              this.translate.instant('common.close'),
              { duration: 3000 }
            );
          },
          error: () =>
            this.snackBar.open(
              this.translate.instant(`${this.i18nPrefix}.failedToUpdate`),
              this.translate.instant('common.close'),
              { duration: 4000 }
            ),
        });
      });
  }
}
