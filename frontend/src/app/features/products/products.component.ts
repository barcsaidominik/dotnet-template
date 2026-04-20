import type { OnInit } from '@angular/core';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DecimalPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import type { PageEvent } from '@angular/material/paginator';
import { MatPaginatorModule } from '@angular/material/paginator';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { Router } from '@angular/router';
import { from } from 'rxjs';
import { ProductsService } from '../../generated/products-client/services/products.service';
import type { ProductImportResultDto } from '../../generated/products-client/models/product-import-result-dto';
import { AuthService } from '../../core/auth/auth.service';
import type { Product } from '../../core/models/product.model';
import { mapProductDto } from '../../shared/mappers/product.mapper';
import { createViewRefresh$ } from '../../shared/rx/view-refresh.util';
import { downloadBlobFile } from '../../shared/utils/file-download.util';

@NgComponent({
  selector: 'app-product-viewer-create-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    TranslateModule,
  ],
  templateUrl: './product-viewer-create-dialog.component.html',
  styleUrls: ['./product-viewer-create-dialog.component.scss'],
})
export class ProductViewerCreateDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<ProductViewerCreateDialogComponent>);
  private readonly fb = ngInject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
  });

  confirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.getRawValue());
    }
  }
}

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
    TranslateModule,
  ],
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.scss'],
})
export class ProductsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly productsApi = inject(ProductsService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  readonly auth = inject(AuthService);

  readonly products = signal<Product[]>([]);
  readonly isLoading = signal(true);
  readonly displayedColumns = ['name', 'price', 'createdAt', 'id', 'actions'];
  readonly totalCount = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(20);
  readonly isExporting = signal(false);
  readonly isImporting = signal(false);
  readonly downloadingProductId = signal<string | null>(null);

  ngOnInit(): void {
    this.loadProducts();

    createViewRefresh$(this.router, '/products')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadProducts());
  }

  private loadProducts(): void {
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
          this.translate.instant('products.failedToLoad'),
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

  openCreateDialog(): void {
    const ref = this.dialog.open(ProductViewerCreateDialogComponent, { width: '360px' });

    ref.afterClosed().subscribe((result) => {
      if (!result) {
        return;
      }
      from(
        this.productsApi.apiProductsPost$Json({
          body: {
            name: result.name,
            price: result.price,
          },
        })
      ).subscribe({
        next: () => {
          this.loadProducts();
          this.snackBar.open(
            this.translate.instant('products.productCreated'),
            this.translate.instant('common.close'),
            { duration: 3000 }
          );
        },
        error: () =>
          this.snackBar.open(
            this.translate.instant('products.failedToCreate'),
            this.translate.instant('common.close'),
            { duration: 4000 }
          ),
      });
    });
  }

  exportProducts(): void {
    this.isExporting.set(true);
    from(this.productsApi.apiProductsExportGet$Response()).subscribe({
      next: (response) => {
        downloadBlobFile(response.body as Blob, response.headers, 'products.xlsx');
        this.isExporting.set(false);
      },
      error: () => {
        this.isExporting.set(false);
        this.snackBar.open(
          this.translate.instant('products.failedToExport'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  importProducts(fileInput: HTMLInputElement): void {
    const file = fileInput.files?.item(0);

    if (!file) {
      return;
    }

    this.isImporting.set(true);
    from(
      this.productsApi.apiProductsImportPost$Json({
        body: { File: file },
      })
    ).subscribe({
      next: (result) => {
        this.isImporting.set(false);
        this.loadProducts();
        this.snackBar.open(
          this.buildImportSummary(result),
          this.translate.instant('common.close'),
          { duration: 7000 }
        );
        fileInput.value = '';
      },
      error: () => {
        this.isImporting.set(false);
        fileInput.value = '';
        this.snackBar.open(
          this.translate.instant('products.failedToImport'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  downloadOrderPdf(product: Product): void {
    this.downloadingProductId.set(product.id);
    from(this.productsApi.apiProductsIdOrderPdfGet$Response({ id: product.id })).subscribe({
      next: (response) => {
        downloadBlobFile(
          response.body as Blob,
          response.headers,
          `${product.name}-order-template.pdf`
        );
        this.downloadingProductId.set(null);
      },
      error: () => {
        this.downloadingProductId.set(null);
        this.snackBar.open(
          this.translate.instant('products.failedToDownloadPdf'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  private buildImportSummary(result: ProductImportResultDto): string {
    const errorCount = result.errors.length;

    return this.translate.instant('products.importCompleted', {
      importedCount: result.importedCount,
      skippedCount: result.skippedCount,
      errorCount,
    });
  }
}
