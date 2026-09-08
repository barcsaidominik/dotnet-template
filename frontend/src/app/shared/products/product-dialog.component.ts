import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';

export interface ProductDialogData {
  mode: 'create' | 'update';
  prefix: string;
  name?: string;
  price?: number;
  quantity?: number;
  minPrice?: number;
}

@Component({
  selector: 'app-product-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    TranslatePipe,
  ],
  templateUrl: './product-dialog.component.html',
  styleUrls: ['./product-dialog.component.scss'],
})
export class ProductDialogComponent {
  readonly dialogRef = inject(MatDialogRef<ProductDialogComponent>);
  readonly data = inject<ProductDialogData>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly dialogKey = `${this.data.prefix}.${this.data.mode === 'create' ? 'createDialog' : 'updateDialog'}`;
  readonly errorPrefix = `${this.data.prefix}.createDialog`;

  readonly form = this.fb.nonNullable.group({
    name: [this.data.name ?? '', Validators.required],
    price: [this.data.price ?? 0, [Validators.required, Validators.min(this.data.minPrice ?? 0)]],
    quantity: [this.data.quantity ?? 0, [Validators.required, Validators.min(0)]],
  });

  confirm(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.getRawValue());
    }
  }
}
