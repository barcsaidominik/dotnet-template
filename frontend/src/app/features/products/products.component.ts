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
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { from } from 'rxjs';
import { ProductsService } from '../../generated/client/services/products.service';
import { AuthService } from '../../core/auth/auth.service';
import { Product } from '../../core/models/product.model';

@NgComponent({
  selector: 'app-product-viewer-create-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './product-viewer-create-dialog.component.html',
  styleUrls: ['./product-viewer-create-dialog.component.scss']
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
  ],
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.scss']
})
export class ProductsPageComponent implements OnInit {
  private readonly productsApi = inject(ProductsService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly auth = inject(AuthService);

  readonly products = signal<Product[]>([]);
  readonly isLoading = signal(true);
  readonly displayedColumns = ['name', 'price', 'createdAt', 'id'];
  readonly totalCount = signal(0);
  readonly page = signal(0);
  readonly pageSize = signal(20);

  ngOnInit(): void {
    this.loadProducts();
  }

  private loadProducts(): void {
    this.isLoading.set(true);
    from(this.productsApi.apiProductsGet$Json({
      page: this.page() + 1,
      pageSize: this.pageSize()
    })).subscribe({
      next: result => {
        this.totalCount.set(Number(result.totalCount ?? 0));
        this.products.set((result.items ?? []).map(p => ({
          id: p.id ?? '',
          name: p.name ?? '',
          price: typeof p.price === 'number' ? p.price : parseFloat(p.price ?? '0'),
          facilityId: p.facilityId ?? '',
          createdAt: p.createdAt ?? '',
        })));
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Failed to load products', 'Close', { duration: 4000 });
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.loadProducts();
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(ProductViewerCreateDialogComponent, { width: '360px' });

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      from(this.productsApi.apiProductsPost$Json({
        body: {
          name: result.name,
          price: result.price
        }
      })).subscribe({
        next: () => {
          this.loadProducts();
          this.snackBar.open('Product created', 'Close', { duration: 3000 });
        },
        error: () => this.snackBar.open('Failed to create product', 'Close', { duration: 4000 })
      });
    });
  }
}
