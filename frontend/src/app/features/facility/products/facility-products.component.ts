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
import { MatTooltipModule } from '@angular/material/tooltip';
import { Component as NgComponent, inject as ngInject } from '@angular/core';
import { from } from 'rxjs';
import { ProductsService } from '../../../generated/client/services/products.service';
import { Product } from '../../../core/models/product.model';

@NgComponent({
  selector: 'app-product-create-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  templateUrl: './product-create-dialog.component.html',
  styleUrls: ['./product-create-dialog.component.scss']
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
  ],
  templateUrl: './facility-products.component.html',
  styleUrls: ['./facility-products.component.scss']
})
export class FacilityProductsPageComponent implements OnInit {
  private readonly productsApi = inject(ProductsService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly products = signal<Product[]>([]);
  readonly isLoading = signal(false);

  readonly displayedColumns = ['name', 'price', 'createdAt', 'id'];

  ngOnInit(): void {
    this.isLoading.set(false);
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(ProductCreateDialogComponent, { width: '360px' });

    ref.afterClosed().subscribe(result => {
      if (!result) return;
      from(this.productsApi.apiProductsPost$Json({
        body: {
          name: result.name,
          price: result.price
        }
      })).subscribe({
        next: id => {
          const newProduct: Product = {
            id,
            name: result.name,
            price: result.price,
            facilityId: '',
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
