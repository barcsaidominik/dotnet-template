import type { Product } from '../../core/models/product.model';

interface ProductLikeDto {
  id?: string | null;
  name?: string | null;
  price?: number | string | null;
  quantity?: number | string | null;
  facilityId?: string | null;
  createdAt?: string | null;
  rowVersion?: number | string | null;
}

export function mapProductDto(dto: ProductLikeDto): Product {
  return {
    id: dto.id ?? '',
    name: dto.name ?? '',
    price: typeof dto.price === 'number' ? dto.price : parseFloat(dto.price ?? '0'),
    quantity: typeof dto.quantity === 'number' ? dto.quantity : parseInt(dto.quantity ?? '0', 10),
    facilityId: dto.facilityId ?? '',
    createdAt: dto.createdAt ?? '',
    rowVersion: typeof dto.rowVersion === 'number' ? dto.rowVersion : parseInt(dto.rowVersion ?? '0', 10),
  };
}
