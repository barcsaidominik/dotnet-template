import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ProductsService } from '../../../generated/products-client/services/products.service';
import { LanguageService } from '../../../core/i18n/language.service';
import { FacilityProductsPageComponent } from './facility-products.component';

const { downloadBlobFileMock } = vi.hoisted(() => ({
  downloadBlobFileMock: vi.fn(),
}));

vi.mock('../../../shared/utils/file-download.util', () => ({
  downloadBlobFile: downloadBlobFileMock,
}));

vi.mock('../../../shared/rx/view-refresh.util', () => ({
  createViewRefresh$: vi.fn(() => of(new Event('refresh'))),
}));

describe('FacilityProductsPageComponent', () => {
  let productsApi: {
    apiProductsGet$Json: ReturnType<typeof vi.fn>;
    apiProductsPost$Json: ReturnType<typeof vi.fn>;
    apiProductsIdPut: ReturnType<typeof vi.fn>;
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

  beforeEach(() => {
    downloadBlobFileMock.mockReset();

    productsApi = {
      apiProductsGet$Json: vi.fn(),
      apiProductsPost$Json: vi.fn(),
      apiProductsIdPut: vi.fn(),
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

    TestBed.configureTestingModule({
      providers: [
        { provide: ProductsService, useValue: productsApi },
        { provide: MatDialog, useValue: dialog },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
        { provide: Router, useValue: { events: of(), navigateByUrl: vi.fn() } },
        {
          provide: LanguageService,
          useValue: { currentLanguage: signal('hu-HU'), dateLocale: signal('hu-HU') },
        },
      ],
    });
  });

  it('loads facility products on init with default pagination', async () => {
    productsApi.apiProductsGet$Json.mockResolvedValue({
      totalCount: 1,
      items: [
        {
          id: '1',
          name: 'Facility Widget',
          price: 12.5,
          quantity: 0,
          facilityId: 'facility-1',
          createdAt: '2026-04-19T18:00:00Z',
          rowVersion: 1,
        },
      ],
    });
    const component = TestBed.runInInjectionContext(() => new FacilityProductsPageComponent());

    component.ngOnInit();
    await Promise.resolve();
    await Promise.resolve();

    expect(productsApi.apiProductsGet$Json).toHaveBeenCalledWith({ page: 1, pageSize: 20 });
    expect(component.products()).toHaveLength(1);
    expect(component.totalCount()).toBe(1);
    expect(component.isLoading()).toBe(false);
  });

  it('creates a product from dialog result and shows success feedback', async () => {
    dialog.open.mockReturnValue({
      afterClosed: () =>
        of({
          name: 'Created Product',
          price: 15,
          quantity: 0,
        }),
    });
    productsApi.apiProductsPost$Json.mockResolvedValue('new-id');
    productsApi.apiProductsGet$Json.mockResolvedValue({ totalCount: 0, items: [] });
    const component = TestBed.runInInjectionContext(() => new FacilityProductsPageComponent());

    component.openCreateDialog();
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(productsApi.apiProductsPost$Json).toHaveBeenCalledWith({
      body: {
        name: 'Created Product',
        price: 15,
        quantity: 0,
      },
    });
    expect(snackBar.open).toHaveBeenCalledWith('facility.products.productCreated', 'common.close', {
      duration: 3000,
    });
  });

  it('updates a product from dialog result and shows success feedback', async () => {
    dialog.open.mockReturnValue({
      afterClosed: () =>
        of({
          name: 'Updated Product',
          price: 22,
          quantity: 5,
        }),
    });
    productsApi.apiProductsIdPut.mockResolvedValue(undefined);
    productsApi.apiProductsGet$Json.mockResolvedValue({ totalCount: 0, items: [] });
    const component = TestBed.runInInjectionContext(() => new FacilityProductsPageComponent());

    component.openEditDialog({
      id: 'product-1',
      name: 'Old Product',
      price: 20,
      quantity: 2,
      facilityId: 'facility-1',
      createdAt: '2026-04-19T18:00:00Z',
      rowVersion: 7,
    });
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();

    expect(productsApi.apiProductsIdPut).toHaveBeenCalledWith({
      id: 'product-1',
      body: {
        name: 'Updated Product',
        price: 22,
        quantity: 5,
        rowVersion: 7,
      },
    });
    expect(snackBar.open).toHaveBeenCalledWith('facility.products.productUpdated', 'common.close', {
      duration: 3000,
    });
  });

  it('imports products, reloads the list and shows the translated summary', async () => {
    productsApi.apiProductsImportPost$Json.mockResolvedValue({
      importedCount: 3,
      skippedCount: 1,
      errors: [{ rowNumber: 2, message: 'Invalid' }],
    });
    productsApi.apiProductsGet$Json.mockResolvedValue({ totalCount: 0, items: [] });
    const component = TestBed.runInInjectionContext(() => new FacilityProductsPageComponent());
    const file = new File(['xlsx'], 'facility-products.xlsx');
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
      'facility.products.importCompleted:{"importedCount":3,"skippedCount":1,"errorCount":1}',
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
    const component = TestBed.runInInjectionContext(() => new FacilityProductsPageComponent());

    component.downloadOrderPdf({
      id: 'product-1',
      name: 'Facility Widget',
      price: 15,
      quantity: 0,
      facilityId: 'facility-1',
      createdAt: '2026-04-19T18:00:00Z',
      rowVersion: 1,
    });

    expect(component.downloadingProductId()).toBe('product-1');

    await Promise.resolve();
    await Promise.resolve();

    expect(downloadBlobFileMock).toHaveBeenCalledWith(
      expect.any(Blob),
      headers,
      'Facility Widget-order-template.pdf'
    );
    expect(component.downloadingProductId()).toBeNull();
  });
});
