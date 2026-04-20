import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ProductDialogComponent } from './product-dialog.component';
import type { ProductDialogData } from './product-dialog.component';

describe('ProductDialogComponent', () => {
  let dialogRef: { close: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    dialogRef = { close: vi.fn() };
  });

  function setup(data: ProductDialogData) {
    TestBed.configureTestingModule({
      providers: [
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    });
    return TestBed.runInInjectionContext(() => new ProductDialogComponent());
  }

  it('dialogKey resolves to createDialog for mode=create', () => {
    const component = setup({ mode: 'create', prefix: 'products' });

    expect(component.dialogKey).toBe('products.createDialog');
  });

  it('dialogKey resolves to updateDialog for mode=update', () => {
    const component = setup({ mode: 'update', prefix: 'facility.products' });

    expect(component.dialogKey).toBe('facility.products.updateDialog');
  });

  it('errorPrefix always points to createDialog', () => {
    const component = setup({ mode: 'update', prefix: 'facility.products' });

    expect(component.errorPrefix).toBe('facility.products.createDialog');
  });

  it('form initialises empty for create mode', () => {
    const component = setup({ mode: 'create', prefix: 'products' });

    expect(component.form.getRawValue()).toEqual({ name: '', price: 0, quantity: 0 });
  });

  it('form is pre-populated with name, price and quantity for update mode', () => {
    const component = setup({
      mode: 'update',
      prefix: 'facility.products',
      name: 'Widget',
      price: 9.99,
      quantity: 5,
    });

    expect(component.form.getRawValue()).toEqual({ name: 'Widget', price: 9.99, quantity: 5 });
  });

  it('confirm() does nothing when form is invalid', () => {
    const component = setup({ mode: 'create', prefix: 'products' });
    component.form.setValue({ name: '', price: 0, quantity: 0 });

    component.confirm();

    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('confirm() closes dialog with form values when form is valid', () => {
    const component = setup({ mode: 'create', prefix: 'products' });
    component.form.setValue({ name: 'Widget', price: 5, quantity: 3 });

    component.confirm();

    expect(dialogRef.close).toHaveBeenCalledWith({ name: 'Widget', price: 5, quantity: 3 });
  });
});
