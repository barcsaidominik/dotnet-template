import { Component, inject, computed } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/auth/auth.service';

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
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
  ],
  styles: [`
    .shell-container {
      display: flex;
      flex-direction: column;
      height: 100vh;
    }
    mat-toolbar {
      position: sticky;
      top: 0;
      z-index: 100;
    }
    .sidenav-container {
      flex: 1;
    }
    mat-sidenav {
      width: 220px;
    }
    .sidenav-content {
      padding: 16px;
    }
    mat-nav-list a {
      border-radius: 4px;
      margin-bottom: 4px;
    }
    .active-link {
      background-color: rgba(0, 0, 0, 0.08);
    }
  `],
  template: `
    <div class="shell-container">
      <mat-toolbar color="primary">
        <span>Template App</span>
        <span class="spacer"></span>
        <button mat-icon-button (click)="auth.logout()" title="Logout">
          <mat-icon>logout</mat-icon>
        </button>
      </mat-toolbar>

      <mat-sidenav-container class="sidenav-container">
        <mat-sidenav mode="side" opened>
          <mat-nav-list>
            @for (item of navItems(); track item.route) {
              <a mat-list-item
                 [routerLink]="item.route"
                 routerLinkActive="active-link">
                <mat-icon matListItemIcon>{{ item.icon }}</mat-icon>
                <span matListItemTitle>{{ item.label }}</span>
              </a>
            }
          </mat-nav-list>
        </mat-sidenav>

        <mat-sidenav-content class="sidenav-content">
          <router-outlet />
        </mat-sidenav-content>
      </mat-sidenav-container>
    </div>
  `
})
export class ShellComponent {
  readonly auth = inject(AuthService);

  readonly navItems = computed<NavItem[]>(() => {
    const role = this.auth.role();
    if (role === 'SystemAdmin') {
      return [
        { label: 'Users', route: '/admin/users', icon: 'people' },
        { label: 'Facilities', route: '/admin/facilities', icon: 'business' },
      ];
    }
    if (role === 'FacilityAdmin') {
      return [
        { label: 'Users', route: '/facility/users', icon: 'people' },
        { label: 'Products', route: '/facility/products', icon: 'inventory_2' },
      ];
    }
    return [
      { label: 'Products', route: '/products', icon: 'inventory_2' },
    ];
  });
}
