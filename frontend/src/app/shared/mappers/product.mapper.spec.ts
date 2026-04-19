import { mapProductDto } from './product.mapper';

describe('mapProductDto', () => {
  it('maps nullable dto fields to safe frontend defaults', () => {
    const result = mapProductDto({});

    expect(result).toEqual({
      id: '',
      name: '',
      price: 0,
      facilityId: '',
      createdAt: '',
    });
  });

  it('preserves numeric price values without conversion loss', () => {
    const result = mapProductDto({
      id: 'product-1',
      name: 'Widget',
      price: 42.5,
      facilityId: 'facility-1',
      createdAt: '2026-04-19T18:30:00Z',
    });

    expect(result).toEqual({
      id: 'product-1',
      name: 'Widget',
      price: 42.5,
      facilityId: 'facility-1',
      createdAt: '2026-04-19T18:30:00Z',
    });
  });

  it('parses string price values for generated api payloads', () => {
    const result = mapProductDto({
      name: 'Widget',
      price: '99.9',
    });

    expect(result.price).toBe(99.9);
  });
});
