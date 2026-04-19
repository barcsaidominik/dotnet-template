import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: '', pathMatch: 'full' },
  {
    path: 'auth',
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.AuthLoginComponent) },
      { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.AuthRegisterComponent) },
      { path: 'set-password', loadComponent: () => import('./features/auth/set-password/set-password.component').then(m => m.AuthSetPasswordComponent) },
    ]
  },
  {
    path: '',
    loadComponent: () => import('./shared/shell/shell.component').then(m => m.AppShellComponent),
    canActivate: [authGuard],
    children: [
      {
        path: 'admin',
        canActivate: [roleGuard(['SystemAdmin'])],
        children: [
          { path: 'users', loadComponent: () => import('./features/admin/users/admin-users.component').then(m => m.AdminUsersPageComponent) },
          { path: 'facilities', loadComponent: () => import('./features/admin/facilities/admin-facilities.component').then(m => m.AdminFacilitiesPageComponent) },
          { path: '', redirectTo: 'users', pathMatch: 'full' }
        ]
      },
      {
        path: 'facility',
        canActivate: [roleGuard(['FacilityAdmin'])],
        children: [
          { path: 'users', loadComponent: () => import('./features/facility/users/facility-users.component').then(m => m.FacilityUsersPageComponent) },
          { path: 'products', loadComponent: () => import('./features/facility/products/facility-products.component').then(m => m.FacilityProductsPageComponent) },
          { path: '', redirectTo: 'users', pathMatch: 'full' }
        ]
      },
      {
        path: 'products',
        canActivate: [roleGuard(['FacilityEditor', 'FacilityViewer'])],
        loadComponent: () => import('./features/products/products.component').then(m => m.ProductsPageComponent)
      }
    ]
  },
  { path: '**', redirectTo: '/auth/login' }
];
