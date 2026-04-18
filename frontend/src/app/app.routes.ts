import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/products', pathMatch: 'full' },
  {
    path: 'auth',
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
      { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
      { path: 'set-password', loadComponent: () => import('./features/auth/set-password/set-password.component').then(m => m.SetPasswordComponent) },
    ]
  },
  {
    path: '',
    loadComponent: () => import('./shared/shell/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'admin',
        canActivate: [roleGuard(['SystemAdmin'])],
        children: [
          { path: 'users', loadComponent: () => import('./features/admin/users/admin-users.component').then(m => m.AdminUsersComponent) },
          { path: 'facilities', loadComponent: () => import('./features/admin/facilities/admin-facilities.component').then(m => m.AdminFacilitiesComponent) },
          { path: '', redirectTo: 'users', pathMatch: 'full' }
        ]
      },
      {
        path: 'facility',
        canActivate: [roleGuard(['FacilityAdmin', 'SystemAdmin'])],
        children: [
          { path: 'users', loadComponent: () => import('./features/facility/users/facility-users.component').then(m => m.FacilityUsersComponent) },
          { path: 'products', loadComponent: () => import('./features/facility/products/facility-products.component').then(m => m.FacilityProductsComponent) },
          { path: '', redirectTo: 'users', pathMatch: 'full' }
        ]
      },
      {
        path: 'products',
        canActivate: [roleGuard(['FacilityAdmin', 'FacilityEditor', 'FacilityViewer'])],
        loadComponent: () => import('./features/products/products.component').then(m => m.ProductsComponent)
      }
    ]
  },
  { path: '**', redirectTo: '/auth/login' }
];
