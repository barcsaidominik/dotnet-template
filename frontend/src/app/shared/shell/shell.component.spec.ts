import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { MailboxService } from '../../core/mailbox/mailbox.service';
import { AppShellComponent } from './shell.component';

describe('AppShellComponent', () => {
  let auth: {
    role: ReturnType<typeof signal>;
  };
  let mailbox: {
    unreadCount: ReturnType<typeof signal>;
    refreshUnreadCount: ReturnType<typeof vi.fn>;
  };
  let snackBar: {
    open: ReturnType<typeof vi.fn>;
  };
  let translate: {
    instant: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    auth = {
      role: signal<string | null>(null),
    };
    mailbox = {
      unreadCount: signal(0),
      refreshUnreadCount: vi.fn(),
    };
    snackBar = {
      open: vi.fn(),
    };
    translate = {
      instant: vi.fn((key: string, params?: { count: number }) =>
        params ? `${key}:${params.count}` : key
      ),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: MailboxService, useValue: mailbox },
        { provide: MatSnackBar, useValue: snackBar },
        { provide: TranslateService, useValue: translate },
      ],
    });
  });

  it('builds system admin navigation items', () => {
    auth.role.set('SystemAdmin');
    const component = TestBed.runInInjectionContext(() => new AppShellComponent());

    expect(component.navItems()).toEqual([
      { label: 'nav.users', route: '/admin/users', icon: 'people' },
      { label: 'nav.facilities', route: '/admin/facilities', icon: 'business' },
      { label: 'nav.mailbox', route: '/mailbox', icon: 'mail' },
    ]);
  });

  it('builds facility admin navigation items', () => {
    auth.role.set('FacilityAdmin');
    const component = TestBed.runInInjectionContext(() => new AppShellComponent());

    expect(component.navItems()).toEqual([
      { label: 'nav.users', route: '/facility/users', icon: 'people' },
      { label: 'nav.products', route: '/facility/products', icon: 'inventory_2' },
      { label: 'nav.mailbox', route: '/mailbox', icon: 'mail' },
    ]);
  });

  it('builds viewer navigation items by default', () => {
    auth.role.set('FacilityViewer');
    const component = TestBed.runInInjectionContext(() => new AppShellComponent());

    expect(component.navItems()).toEqual([
      { label: 'nav.products', route: '/products', icon: 'inventory_2' },
      { label: 'nav.mailbox', route: '/mailbox', icon: 'mail' },
    ]);
  });

  it('shows a snackbar when unread notifications increase after the initial load', async () => {
    mailbox.unreadCount.set(2);
    mailbox.refreshUnreadCount.mockResolvedValueOnce(2).mockResolvedValueOnce(5);
    const component = TestBed.runInInjectionContext(() => new AppShellComponent());

    await (
      component as AppShellComponent & { refreshUnreadCount: () => Promise<void> }
    ).refreshUnreadCount();
    await (
      component as AppShellComponent & { refreshUnreadCount: () => Promise<void> }
    ).refreshUnreadCount();

    expect(snackBar.open).toHaveBeenCalledWith('mailbox.newNotifications:3', 'common.close', {
      duration: 4000,
    });
  });

  it('swallows polling failures without surfacing a snackbar', async () => {
    mailbox.refreshUnreadCount.mockRejectedValue(new Error('network'));
    const component = TestBed.runInInjectionContext(() => new AppShellComponent());

    await (
      component as AppShellComponent & { refreshUnreadCount: () => Promise<void> }
    ).refreshUnreadCount();

    expect(snackBar.open).not.toHaveBeenCalled();
  });
});
