import type { OnInit } from '@angular/core';
import { Directive, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { from } from 'rxjs';
import { LanguageService } from '../../core/i18n/language.service';
import { ProductsService } from '../../generated/products-client/services/products.service';
import type { ProductImportResultDto } from '../../generated/products-client/models/product-import-result-dto';
import type { Product } from '../../core/models/product.model';
import { createViewRefresh$ } from '../rx/view-refresh.util';
import { downloadBlobFile } from '../utils/file-download.util';
import { ProductDialogComponent } from './product-dialog.component';
import type { ProductDialogData } from './product-dialog.component';

@Directive({ standalone: true })
export abstract class ProductsBaseComponent implements OnInit {
  protected abstract readonly i18nPrefix: string;
  protected abstract readonly routePath: string;
  protected readonly exportFilename: string = 'products.xlsx';

  protected readonly destroyRef = inject(DestroyRef);
  protected readonly router = inject(Router);
  protected readonly productsApi = inject(ProductsService);
  protected readonly dialog = inject(MatDialog);
  protected readonly snackBar = inject(MatSnackBar);
  protected readonly translate = inject(TranslateService);
  readonly lang = inject(LanguageService);

  readonly products = signal<Product[]>([]);
  readonly isLoading = signal(true);
  readonly isExporting = signal(false);
  readonly isImporting = signal(false);
  readonly downloadingProductId = signal<string | null>(null);
  readonly displayedColumns = ['name', 'price', 'quantity', 'createdAt', 'actions'];
  readonly searchTerm = signal('');
  readonly sortBy = signal<string | null>(null);
  readonly sortDescending = signal(false);

  ngOnInit(): void {
    this.loadProducts();
    createViewRefresh$(this.router, this.routePath)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadProducts());
  }

  abstract loadProducts(): void;

  onSearchChange(value: string): void {
    this.searchTerm.set(value.trim());
    this.loadProducts();
  }

  clearSearch(): void {
    if (!this.searchTerm()) {
      return;
    }

    this.searchTerm.set('');
    this.loadProducts();
  }

  toggleSort(column: 'name' | 'price' | 'createdAt'): void {
    if (this.sortBy() !== column) {
      this.sortBy.set(column);
      this.sortDescending.set(false);
    } else if (!this.sortDescending()) {
      this.sortDescending.set(true);
    } else {
      this.sortBy.set(null);
      this.sortDescending.set(false);
    }

    this.loadProducts();
  }

  sortIcon(column: string): string {
    if (this.sortBy() !== column) {
      return 'unfold_more';
    }

    return this.sortDescending() ? 'south' : 'north';
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(ProductDialogComponent, {
      width: '360px',
      data: { mode: 'create', prefix: this.i18nPrefix } as ProductDialogData,
    });

    ref.afterClosed().subscribe((result: { name: string; price: number; quantity: number } | undefined) => {
      if (!result) {
        return;
      }
      from(
        this.productsApi.apiProductsPost$Json({
          body: { name: result.name, price: result.price, quantity: result.quantity },
        })
      ).subscribe({
        next: () => {
          this.loadProducts();
          this.snackBar.open(
            this.translate.instant(`${this.i18nPrefix}.productCreated`),
            this.translate.instant('common.close'),
            { duration: 3000 }
          );
        },
        error: () =>
          this.snackBar.open(
            this.translate.instant(`${this.i18nPrefix}.failedToCreate`),
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
        downloadBlobFile(response.body as Blob, response.headers, this.exportFilename);
        this.isExporting.set(false);
      },
      error: () => {
        this.isExporting.set(false);
        this.snackBar.open(
          this.translate.instant(`${this.i18nPrefix}.failedToExport`),
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
          this.translate.instant(`${this.i18nPrefix}.failedToImport`),
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
          this.translate.instant(`${this.i18nPrefix}.failedToDownloadPdf`),
          this.translate.instant('common.close'),
          { duration: 4000 }
        );
      },
    });
  }

  protected buildImportSummary(result: ProductImportResultDto): string {
    return this.translate.instant(`${this.i18nPrefix}.importCompleted`, {
      importedCount: result.importedCount,
      skippedCount: result.skippedCount,
      errorCount: result.errors.length,
    });
  }

  protected buildProductsQueryParams(
    page: number,
    pageSize: number
  ): {
    page: number;
    pageSize: number;
    search?: string;
    sortBy?: string;
    sortDescending?: boolean;
  } {
    const params: {
      page: number;
      pageSize: number;
      search?: string;
      sortBy?: string;
      sortDescending?: boolean;
    } = {
      page,
      pageSize,
    };

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
