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
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss']
})
export class AppShellComponent {
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
