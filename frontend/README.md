# Frontend

Angular 20 frontend with standalone components, signals, Material Design, and generated OpenAPI clients.

## Tech Stack

| Component | Technology |
|-----------|-----------|
| **Framework** | Angular 20 |
| **UI Library** | Angular Material 20 |
| **Architecture** | Standalone components, Signals |
| **State Management** | RxJS, Angular Signals |
| **Routing** | Angular Router with guards |
| **i18n** | ngx-translate (Hungarian, English) |
| **API Client** | Generated from OpenAPI (ng-openapi-gen) |
| **Testing** | Vitest 3 |
| **Linting** | ESLint 9 + typescript-eslint |
| **Formatting** | Prettier 3 |
| **Git Hooks** | simple-git-hooks + lint-staged |

## Features

### Authentication & User Management
- Login, registration, password setup
- JWT + refresh token handling
- Auth guard and role guard
- Automatic token refresh on 401
- Admin user approval workflow

### Admin Pages (SystemAdmin role)
- **Users:** List, approve, assign roles, delete, Excel export, update email
- **Facilities:** CRUD, assign users, Excel export
- **Audit Log:** Filter by entity type, action, date range, user

### Facility Pages (FacilityAdmin role)
- **Users:** Create facility users, assign roles, remove users
- **Products:** CRUD with pagination, sort, search, Excel import/export, PDF generation

### Products (FacilityEditor/FacilityViewer roles)
- Read-only product list with pagination and sorting

### Mailbox
- In-app messaging
- Unread count badge
- Mark as read/unread
- Real-time updates

### i18n
- Hungarian and English translations
- Date locale synchronization
- Backend preference sync
- Language switcher in shell

### Theme
- Light and dark mode
- Persistent user preference
- Material Design 3

## Development

### Prerequisites
- Node.js 20+
- npm 10+

### Install Dependencies

```bash
npm install
```

### Development Server

```bash
npm start
```

Application runs at http://localhost:4200 with proxy to backend gateway (http://localhost:8080).

**Proxy configuration:** [proxy.conf.json](proxy.conf.json)

### Build

```bash
npm run build
```

Production build outputs to `dist/` directory.

### Testing

```bash
npm run test
```

**Current status:** 88/88 passing (Vitest unit tests)

**Run once:**
```bash
npm run test:run
```

### Code Quality

**Lint:**
```bash
npm run lint
npm run lint:fix
```

**Format:**
```bash
npm run format
npm run format:check
```

**Full style check:**
```bash
npm run style:check
```

## OpenAPI Client Generation

The frontend uses generated TypeScript clients from backend OpenAPI specs.

### Generate All Clients

```bash
npm run generate:api
```

This runs:
1. `generate:types` - Generates TypeScript types from `Api.json`
2. `generate:client` - Generates Angular services from `Api.json`
3. `generate:products` - Generates types and services from `ProductsApi.json`

### Manual Steps

**Core API:**
```bash
npm run generate:types
npm run generate:client
```

**Products API:**
```bash
npm run generate:products
```

**Configuration files:**
- [ng-openapi-gen.json](ng-openapi-gen.json) - Core API config
- [ng-openapi-gen-products.json](ng-openapi-gen-products.json) - Products API config

**Generated code location:**
- `src/app/generated/client/` - Core API services
- `src/app/generated/products-client/` - Products API services
- `src/app/generated/api.types.ts` - Core API types
- `src/app/generated/products.api.types.ts` - Products API types

**Automated generation:**
```powershell
.\scripts\Refresh-OpenApi.ps1
```
(Starts backend services, fetches specs, generates clients)

## Project Structure

```
src/app/
├── core/                   ← Core services and guards
│   ├── auth/               ← Auth service, guards, interceptors
│   ├── i18n/               ← Language service
│   └── theme/              ← Theme service
├── features/               ← Feature modules
│   ├── admin/              ← Admin pages (users, facilities, audit-log)
│   ├── auth/               ← Auth pages (login, register, set-password)
│   ├── facility/           ← Facility pages (users, products)
│   ├── mailbox/            ← Mailbox page
│   └── products/           ← Products page (FacilityEditor/Viewer)
├── generated/              ← Generated OpenAPI clients
│   ├── client/             ← Core API client
│   ├── products-client/    ← Products API client
│   ├── api.types.ts        ← Core API types
│   └── products.api.types.ts  ← Products API types
├── shared/                 ← Shared components and utilities
│   ├── components/         ← Reusable components
│   ├── shell/              ← Shell layout with nav
│   └── utils/              ← Utility functions, mappers
└── app.routes.ts           ← Application routing
```

## Routes

| Path | Component | Guard | Description |
|------|-----------|-------|-------------|
| `/auth/login` | AuthLoginComponent | - | Login page |
| `/auth/register` | AuthRegisterComponent | - | Registration page |
| `/auth/set-password` | AuthSetPasswordComponent | - | Password setup (facility users) |
| `/admin/users` | AdminUsersPageComponent | authGuard, roleGuard(SystemAdmin) | User management |
| `/admin/facilities` | AdminFacilitiesPageComponent | authGuard, roleGuard(SystemAdmin) | Facility management |
| `/admin/audit-log` | AdminAuditLogPageComponent | authGuard, roleGuard(SystemAdmin) | Audit log viewer |
| `/facility/users` | FacilityUsersPageComponent | authGuard, roleGuard(FacilityAdmin) | Facility user management |
| `/facility/products` | FacilityProductsPageComponent | authGuard, roleGuard(FacilityAdmin) | Facility product management |
| `/products` | ProductsPageComponent | authGuard, roleGuard(FacilityEditor, FacilityViewer) | Product list (read-only) |
| `/mailbox` | MailboxPageComponent | authGuard | Mailbox messages |

**Route definitions:** [app.routes.ts](src/app/app.routes.ts)

## i18n (Internationalization)

**Supported languages:**
- Hungarian (hu)
- English (en)

**Translation files:**
- [src/assets/i18n/hu.json](src/assets/i18n/hu.json)
- [src/assets/i18n/en.json](src/assets/i18n/en.json)

**Usage in templates:**
```html
<h1>{{ 'auth.login.title' | translate }}</h1>
```

**Usage in code:**
```typescript
this.translateService.instant('auth.login.success');
```

**Language switching:**
- User can switch via shell dropdown
- Preference saved to backend
- Date locale synced automatically

## Theme System

**ThemeService:**
- Light/dark mode toggle
- Persistent preference (localStorage)
- Material Design 3 theming

**Usage:**
```typescript
this.themeService.toggleTheme();
this.themeService.isDark(); // Signal<boolean>
```

## Code Style & Git Hooks

**Configuration:**
- [.editorconfig](.editorconfig) - Editor settings
- [eslint.config.js](eslint.config.js) - ESLint rules
- [.prettierrc.json](.prettierrc.json) - Prettier formatting

**Pre-commit hook:**
- Runs `lint-staged` on staged files
- ESLint + Prettier on `.ts` and `.html`
- Prettier only on `.scss`, `.css`, `.json`, `.js`, `.md`

**Setup:**
```bash
npm run prepare
```

## Testing Strategy

**Vitest configuration:** [vitest.config.ts](vitest.config.ts)

**Test coverage:**
- Auth service and guards
- Language service
- Theme service
- Mailbox components and service
- Admin pages (users, facilities)
- Facility pages (users, products)
- Products page
- Shell component
- Mappers and utilities

**Run tests:**
```bash
npm run test          # Watch mode
npm run test:run      # Run once
```

## Docker

**Dockerfile:** [Dockerfile](Dockerfile)

**Build:**
```bash
docker build -t template-frontend .
```

**Run:**
```bash
docker run -p 80:80 template-frontend
```

**Docker Compose:**
```bash
docker compose up -d --build
```

Frontend is served via nginx with reverse proxy to backend gateway.

**nginx configuration:** [nginx.conf](nginx.conf)

## Security Headers (nginx)

- `Content-Security-Policy` - Restricts resource loading
- `X-Frame-Options: DENY` - Prevents clickjacking
- `X-Content-Type-Options: nosniff` - Prevents MIME sniffing
- `Referrer-Policy: strict-origin-when-cross-origin` - Referrer control
- `Permissions-Policy` - Disables unnecessary browser features

## Environment Configuration

Frontend uses Angular's environment system.

**Development:**
- API proxy to `http://localhost:8080`
- Source maps enabled
- Development mode

**Production:**
- API base URL: `/api` (served by nginx)
- Optimized build
- AOT compilation

## NPM Scripts

| Script | Purpose |
|--------|---------|
| `npm start` | Development server (http://localhost:4200) |
| `npm run build` | Production build |
| `npm run watch` | Watch mode build |
| `npm run test` | Vitest watch mode |
| `npm run test:run` | Vitest run once |
| `npm run lint` | ESLint check |
| `npm run lint:fix` | ESLint auto-fix |
| `npm run format` | Prettier format |
| `npm run format:check` | Prettier check |
| `npm run style:check` | Full style gate (format + lint) |
| `npm run generate:api` | Generate all OpenAPI clients |
| `npm run generate:types` | Generate Core API types |
| `npm run generate:client` | Generate Core API services |
| `npm run generate:products` | Generate Products API client |
| `npm run prepare` | Setup git hooks |

## Notes

- **Standalone components:** No NgModules, uses Angular 20 standalone APIs
- **Signals:** State management with Angular Signals where applicable
- **Material Design 3:** Latest Material components and theming
- **OpenAPI-first:** All API communication via generated clients
- **Type-safe:** Full TypeScript coverage with strict mode
- **Test coverage:** 88 unit tests covering core functionality
