/// <reference types="vitest/globals" />
import { NavigationEnd, type Router } from '@angular/router';
import { Subject } from 'rxjs';
import { createViewRefresh$ } from './view-refresh.util';

describe('createViewRefresh$', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('emits when the window regains focus', () => {
    const events$ = new Subject<Event>();
    const router = { events: events$.asObservable() } as Router;
    const emissions: Event[] = [];

    createViewRefresh$(router, '/products').subscribe((event) => emissions.push(event));

    window.dispatchEvent(new Event('focus'));
    vi.advanceTimersByTime(100);

    expect(emissions).toHaveLength(1);
  });

  it('emits only for navigation events matching the route prefix', () => {
    const events$ = new Subject<Event>();
    const router = { events: events$.asObservable() } as Router;
    const emissions: Event[] = [];

    createViewRefresh$(router, '/products').subscribe((event) => emissions.push(event));

    events$.next(new NavigationEnd(1, '/admin/users', '/admin/users'));
    vi.advanceTimersByTime(100);
    events$.next(new NavigationEnd(2, '/products', '/products?page=2'));
    vi.advanceTimersByTime(100);

    expect(emissions).toHaveLength(1);
    expect(emissions[0]).toBeInstanceOf(NavigationEnd);
  });
});
