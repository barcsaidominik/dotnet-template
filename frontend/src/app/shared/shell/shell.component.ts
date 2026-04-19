import { Component, DestroyRef, OnInit, computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatBadgeModule } from '@angular/material/badge';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { timer } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { MailboxService } from '../../core/mailbox/mailbox.service';
import { LanguageSwitcherComponent } from '../language-switcher/language-switcher.component';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatBadgeModule,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatSnackBarModule,
    TranslateModule,
    LanguageSwitcherComponent,
  ],
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss']
})
export class AppShellComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly mailbox = inject(MailboxService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly snackBar = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);
  private hasLoadedUnreadCount = false;

  readonly navItems = computed<NavItem[]>(() => {
    const role = this.auth.role();
    if (role === 'SystemAdmin') {
      return [
        { label: 'nav.users', route: '/admin/users', icon: 'people' },
        { label: 'nav.facilities', route: '/admin/facilities', icon: 'business' },
        { label: 'nav.mailbox', route: '/mailbox', icon: 'mail' },
      ];
    }
    if (role === 'FacilityAdmin') {
      return [
        { label: 'nav.users', route: '/facility/users', icon: 'people' },
        { label: 'nav.products', route: '/facility/products', icon: 'inventory_2' },
        { label: 'nav.mailbox', route: '/mailbox', icon: 'mail' },
      ];
    }
    return [
      { label: 'nav.products', route: '/products', icon: 'inventory_2' },
      { label: 'nav.mailbox', route: '/mailbox', icon: 'mail' },
    ];
  });

  ngOnInit(): void {
    timer(0, 30000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.refreshUnreadCount());
  }

  private async refreshUnreadCount(): Promise<void> {
    try {
      const previousCount = this.mailbox.unreadCount();
      const unreadCount = await this.mailbox.refreshUnreadCount();

      if (this.hasLoadedUnreadCount && unreadCount > previousCount) {
        this.snackBar.open(
          this.translate.instant('mailbox.newNotifications', { count: unreadCount - previousCount }),
          this.translate.instant('common.close'),
          { duration: 4000 });
      }

      this.hasLoadedUnreadCount = true;
    } catch {
      // Keep the shell resilient if polling fails.
    }
  }
}
