import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { ProductsService } from '../../generated/client/services/products.service';
import { ProductsPageComponent } from './products.component';

const { downloadBlobFileMock } = vi.hoisted(() => ({
  downloadBlobFileMock: vi.fn(),
}));

vi.mock('../../shared/utils/file-download.util', () => ({
  downloadBlobFile: downloadBlobFileMock,
}));

vi.mock('../../shared/rx/view-refresh.util', () => ({
  createViewRefresh$: vi.fn(() => of(new Event('refresh'))),
}));

describe('ProductsPageComponent', () => {
  let productsApi: {
    apiProductsGet$Json: ReturnType<typeof vi.fn>;
    apiProductsPost$Json: ReturnType<typeof vi.fn>;
    apiProductsExportGet$Response: ReturnType<typeof vi.fn>;
    apiProductsImportPost$Json: ReturnType<typeof vi.fn>;
    apiProductsIdOrderPdfGet$Response: ReturnType<typeof vi.fn>;
  };
  let dialog: {
    open: ReturnType<typeof vi.fn>;
  };
  let snackBar: {
    open: ReturnType<typeof vi.fn>;
  };
  let translate: {
    instant: ReturnType<typeof vi.fn>;
  };
  let auth: {
    canEditProducts: ReturnType<typeof signal>;
  };

  beforeEach(() => {
    downloadBlobFileMock.mockReset();

    productsApi = {
      apiProductsGet$Json: vi.fn(),
      apiProductsPost$Json: vi.fn(),
      apiProductsExportGet$Response: vi.fn(),
      apiProductsImportPost$Json: vi.fn(),
      apiProductsIdOrderPdfGet$Response: vi.fn(),
    };
    dialog = {
      open: vi.fn(),
    };
    snackBar = {
      open: vi.fn(),
    };
    translate = {
      instant: vi.fn((key: string, params?: Record<string, unknown>) =>
        params ? `${key}:${JSON.stringify(params)}` : key
      ),
    };
    auth = {
      canEditProducts: signal(true),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: ProductsService, useValue: productsApi },
        { provide: MatDialog, useValue: dialog },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
        { provide: Router, useValue: { events: of(), navigateByUrl: vi.fn() } },
        { provide: AuthService, useValue: auth },
      ],
    });
  });

  it('loads products on init and maps paging metadata', async () => {
    productsApi.apiProductsGet$Json.mockResolvedValue({
      totalCount: 2,
      items: [
        { id: '1', name: 'Widget', price: 10, facilityId: 'f1', createdAt: '2026-04-19T18:00:00Z' },
      ],
    });
    const component = TestBed.runInInjectionContext(() => new ProductsPageComponent());

    component.ngOnInit();
    await Promise.resolve();
    await Promise.resolve();

    expect(productsApi.apiProductsGet$Json).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
    expect(component.totalCount()).toBe(2);
    expect(component.products()).toHaveLength(1);
    expect(component.isLoading()).toBe(false);
  });

  it('updates paging state and reloads products on page change', async () => {
    productsApi.apiProductsGet$Json.mockResolvedValue({ totalCount: 0, items: [] });
    const component = TestBed.runInInjectionContext(() => new ProductsPageComponent());

    component.onPageChange({ pageIndex: 2, pageSize: 50, length: 0, previousPageIndex: 1 });
    await Promise.resolve();
    await Promise.resolve();

    expect(component.page()).toBe(2);
    expect(component.pageSize()).toBe(50);
    expect(productsApi.apiProductsGet$Json).toHaveBeenCalledWith({ page: 3, pageSize: 50 });
  });

  it('exports products and delegates file download', async () => {
    const headers = new Headers() as never;
    productsApi.apiProductsExportGet$Response.mockResolvedValue({
      body: new Blob(['xlsx']),
      headers,
    });
    const component = TestBed.runInInjectionContext(() => new ProductsPageComponent());

    component.exportProducts();
    await Promise.resolve();
    await Promise.resolve();

    expect(downloadBlobFileMock).toHaveBeenCalledWith(expect.any(Blob), headers, 'products.xlsx');
    expect(component.isExporting()).toBe(false);
  });

  it('imports products, reloads the list and shows the translated summary', async () => {
    productsApi.apiProductsImportPost$Json.mockResolvedValue({
      importedCount: 2,
      skippedCount: 1,
      errors: [{ rowNumber: 4, message: 'Invalid' }],
    });
    productsApi.apiProductsGet$Json.mockResolvedValue({ totalCount: 0, items: [] });
    const component = TestBed.runInInjectionContext(() => new ProductsPageComponent());
    const file = new File(['xlsx'], 'products.xlsx');
    const fileInput = {
      files: { item: vi.fn().mockReturnValue(file) },
      value: 'has-value',
    } as unknown as HTMLInputElement;

    component.importProducts(fileInput);
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(productsApi.apiProductsImportPost$Json).toHaveBeenCalledWith({
      body: { File: file },
    });
    expect(snackBar.open).toHaveBeenCalledWith(
      'products.importCompleted:{"importedCount":2,"skippedCount":1,"errorCount":1}',
      'common.close',
      { duration: 7000 }
    );
    expect(fileInput.value).toBe('');
  });

  it('downloads order pdf and clears the active loading product id', async () => {
    const headers = new Headers() as never;
    productsApi.apiProductsIdOrderPdfGet$Response.mockResolvedValue({
      body: new Blob(['pdf']),
      headers,
    });
    const component = TestBed.runInInjectionContext(() => new ProductsPageComponent());

    component.downloadOrderPdf({
      id: 'product-1',
      name: 'Widget',
      price: 15,
      facilityId: 'facility-1',
      createdAt: '2026-04-19T18:00:00Z',
    });

    expect(component.downloadingProductId()).toBe('product-1');

    await Promise.resolve();
    await Promise.resolve();

    expect(downloadBlobFileMock).toHaveBeenCalledWith(
      expect.any(Blob),
      headers,
      'Widget-order-template.pdf'
    );
    expect(component.downloadingProductId()).toBeNull();
  });
});
