import { Product } from '../../core/models/product.model';

type ProductLikeDto = {
  id?: string | null;
  name?: string | null;
  price?: number | string | null;
  facilityId?: string | null;
  createdAt?: string | null;
};

export function mapProductDto(dto: ProductLikeDto): Product {
  return {
    id: dto.id ?? '',
    name: dto.name ?? '',
    price: typeof dto.price === 'number' ? dto.price : parseFloat(dto.price ?? '0'),
    facilityId: dto.facilityId ?? '',
    createdAt: dto.createdAt ?? '',
  };
}