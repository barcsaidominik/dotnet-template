import { Component, inject, signal, OnInit } from '@angular/core';
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
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { ProductService } from '../../core/services/product.service';
import { AuthService } from '../../core/auth/auth.service';
import { Product } from '../../core/models/product.model';

@NgComponent({
  selector: 'app-create-product-viewer-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>Create Product</h2>
    <mat-dialog-content>
      <form [formGroup]="form" (ngSubmit)="confirm()">
        <mat-form-field appearance="outline" style="width:100%; margin-top:8px">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name">
          @if (form.get('name')?.hasError('required') && form.get('name')?.touched) {
            <mat-error>Name is required</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline" style="width:100%">
          <mat-label>Price</mat-label>
          <input matInput type="number" formControlName="price" min="0" step="0.01">
          @if (form.get('price')?.hasError('required') && form.get('price')?.touched) {
            <mat-error>Price is required</mat-error>
          }
          @if (form.get('price')?.hasError('min') && form.get('price')?.touched) {
            <mat-error>Price must be 0 or greater</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="confirm()">Create</button>
    </mat-dialog-actions>
  `
})
export class CreateProductViewerDialogComponent {
  readonly dialogRef = ngInject(MatDialogRef<CreateProductViewerDialogComponent>);
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
  ],
  styles: [`
    .header-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }
    table {
      width: 100%;
    }
    .placeholder {
      text-align: center;
      padding: 40px 24px;
      color: #666;
    }
    .placeholder mat-icon {
      font-size: 48px;
      height: 48px;
      width: 48px;
      color: #bbb;
    }
  `],
  template: `
    <div class="header-row">
      <h2>Products</h2>
      @if (auth.canEditProducts()) {
        <button mat-flat-button color="primary" (click)="openCreateDialog()">
          <mat-icon>add</mat-icon> Create Product
        </button>
      }
    </div>

    <mat-card>
      <mat-card-content>
        @if (products().length > 0) {
          <table mat-table [dataSource]="products()">
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Name</th>
              <td mat-cell *matCellDef="let product">{{ product.name }}</td>
            </ng-container>

            <ng-container matColumnDef="price">
              <th mat-header-cell *matHeaderCellDef>Price</th>
              <td mat-cell *matCellDef="let product">{{ product.price | number:'1.2-2' }}</td>
            </ng-container>

            <ng-container matColumnDef="createdAt">
              <th mat-header-cell *matHeaderCellDef>Created</th>
              <td mat-cell *matCellDef="let product">{{ product.createdAt | date:'short' }}</td>
            </ng-container>

            <ng-container matColumnDef="id">
              <th mat-header-cell *matHeaderCellDef>ID</th>
              <td mat-cell *matCellDef="let product">{{ product.id }}</td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
        } @else {
          <div class="placeholder">
            <mat-icon>inventory_2</mat-icon>
            <p>Product list is coming soon.</p>
            @if (auth.canEditProducts()) {
              <p>Use the <strong>Create Product</strong> button above to add your first product.</p>
            }
          </div>
        }
      </mat-card-content>
    </mat-card>
  `
})
export class ProductsComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly auth = inject(AuthService);

  readonly products = signal<Product[]>([]);
  readonly displayedColumns = ['name', 'price', 'createdAt', 'id'];

  ngOnInit(): void {
    // No list endpoint yet - products will appear after creation
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CreateProductViewerDialogComponent, { width: '360px' });

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.productService.createProduct(result.name, result.price).subscribe({
        next: response => {
          const newProduct: Product = {
            id: response.id,
            name: result.name,
            price: result.price,
            facilityId: this.auth.facilityId() ?? '',
            createdAt: new Date().toISOString(),
          };
          this.products.update(list => [...list, newProduct]);
          this.snackBar.open('Product created', 'Close', { duration: 3000 });
        },
        error: () => this.snackBar.open('Failed to create product', 'Close', { duration: 4000 })
      });
    });
  }
}
