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
import { Router } from '@angular/router';
import { from } from 'rxjs';
import { ProductsService } from '../../../generated/products-client/services/products.service';
import type { ProductImportResultDto } from '../../../generated/products-client/models/product-import-result-dto';
import type { Product } from '../../../core/models/product.model';
import { mapProductDto } from '../../../shared/mappers/product.mapper';
import { createViewRefresh$ } from '../../../shared/rx/view-refresh.util';
import { downloadBlobFile } from '../../../shared/utils/file-download.util';

@NgComponent({
  selector: 'app-product-create-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    TranslateModule,
  ],
  templateUrl: './product-create-dialog.component.html',
  styleUrls: ['./product-create-dialog.component.scss'],
})
export class ProductCreateDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<ProductCreateDialogComponent>);
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

@NgComponent({
  selector: 'app-product-update-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    TranslateModule,
  ],
  templateUrl: './product-update-dialog.component.html',
  styleUrls: ['./product-update-dialog.component.scss'],
})
export class ProductUpdateDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<ProductUpdateDialogComponent>);
  readonly data = ngInject<{ name: string; price: number }>(MAT_DIALOG_DATA);
  private readonly fb = ngInject(FormBuilder);

  readonly form = this.fb.nonNullable.group({
    name: [this.data.name, Validators.required],
    price: [this.data.price, [Validators.required, Validators.min(0.01)]],
  });

  confirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.getRawValue());
    }
  }
}

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
    MatTooltipModule,
    TranslateModule,
  ],
  templateUrl: './facility-products.component.html',
  styleUrls: ['./facility-products.component.scss'],
})
export class FacilityProductsPageComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly productsApi = inject(ProductsService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  readonly products = signal<Product[]>([]);
  readonly isLoading = signal(true);
  readonly isExporting = signal(false);
  readonly isImporting = signal(false);
  readonly downloadingProductId = signal<string | null>(null);

  readonly displayedColumns = ['name', 'price', 'createdAt', 'id', 'actions'];

  ngOnInit(): void {
    this.loadProducts();

    createViewRefresh$(this.router, '/facility/products')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadProducts());
  }

  private loadProducts(): void {
    this.isLoading.set(true);
    from(
      this.productsApi.apiProductsGet$Json({
        page: 1,
        pageSize: 1000,
      })
    ).subscribe({
      next: (result) => {
        this.products.set((result.items ?? []).map(mapProductDto));
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open(
          this.translate.instant('facility.products.failedToLoad'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(ProductCreateDialogComponent, { width: '360px' });

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
            this.translate.instant('facility.products.productCreated'),
            this.translate.instant('common.close'),
            { duration: 3000 }
          );
        },
        error: () =>
          this.snackBar.open(
            this.translate.instant('facility.products.failedToCreate'),
            this.translate.instant('common.close'),
            { duration: 4000 }
          ),
      });
    });
  }

  openEditDialog(product: Product): void {
    const ref = this.dialog.open(ProductUpdateDialogComponent, {
      width: '360px',
      data: { name: product.name, price: product.price },
    });

    ref.afterClosed().subscribe((result) => {
      if (!result) {
        return;
      }
      from(
        this.productsApi.apiProductsIdPut({
          id: product.id,
          body: { name: result.name, price: result.price },
        })
      ).subscribe({
        next: () => {
          this.loadProducts();
          this.snackBar.open(
            this.translate.instant('facility.products.productUpdated'),
            this.translate.instant('common.close'),
            { duration: 3000 }
          );
        },
        error: () =>
          this.snackBar.open(
            this.translate.instant('facility.products.failedToUpdate'),
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
        downloadBlobFile(response.body as Blob, response.headers, 'facility-products.xlsx');
        this.isExporting.set(false);
      },
      error: () => {
        this.isExporting.set(false);
        this.snackBar.open(
          this.translate.instant('facility.products.failedToExport'),
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
          this.translate.instant('facility.products.failedToImport'),
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
          this.translate.instant('facility.products.failedToDownloadPdf'),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  private buildImportSummary(result: ProductImportResultDto): string {
    const errorCount = result.errors.length;

    return this.translate.instant('facility.products.importCompleted', {
      importedCount: result.importedCount,
      skippedCount: result.skippedCount,
      errorCount,
    });
  }
}
