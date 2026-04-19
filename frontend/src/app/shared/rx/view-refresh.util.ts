import type { Router } from '@angular/router';
import { NavigationEnd } from '@angular/router';
import type { Observable } from 'rxjs';
import { auditTime, filter, fromEvent, merge } from 'rxjs';

export function createViewRefresh$(
  router: Router,
  routePrefix: string
): Observable<Event | NavigationEnd> {
  const tabVisible$ = fromEvent(document, 'visibilitychange').pipe(
    filter(() => document.visibilityState === 'visible')
  );
  const windowFocused$ = fromEvent(window, 'focus');
  const routeNavigation$ = router.events.pipe(
    filter((event): event is NavigationEnd => event instanceof NavigationEnd),
    filter((event) => event.urlAfterRedirects.startsWith(routePrefix))
  );

  return merge(tabVisible$, windowFocused$, routeNavigation$).pipe(auditTime(100));
}
