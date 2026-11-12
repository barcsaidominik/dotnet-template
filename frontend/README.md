# Frontend

**English** | [Magyar](README.hu.md)

Angular 22 frontend with standalone components, signals, Material Design and generated OpenAPI clients.

## Tech Stack

| Component            | Technology                              |
| -------------------- | --------------------------------------- |
| **Framework**        | Angular 22                              |
| **UI library**       | Angular Material 22                     |
| **Architecture**     | Standalone components, Signals          |
| **State management** | RxJS, Angular Signals                   |
| **Routing**          | Angular Router with guards              |
| **i18n**             | ngx-translate (Hungarian, English)      |
| **API client**       | Generated from OpenAPI (ng-openapi-gen) |
| **Testing**          | Vitest 4                                |
| **Linting**          | ESLint 9 + typescript-eslint            |
| **Formatting**       | Prettier 3                              |
| **Git hooks**        | simple-git-hooks + lint-staged          |

## Features

### Authentication and user management

- Login, registration, password setup
- JWT + refresh token handling
- Auth guard and role guard
- Automatic token refresh on 401
- Admin user approval workflow

### Admin pages (SystemAdmin role)

- **Users:** list, approval, role assignment, deletion, Excel export, email change
- **Facilities:** CRUD, user assignment, Excel export
- **Audit log:** filtering by entity type, action, date range and user

### Facility pages (FacilityAdmin role)

- **Users:** create facility users, role assignment, removal, search, sorting
- **Products:** CRUD with paging, sorting, search, Excel import/export, PDF generation

### Products (FacilityEditor/FacilityViewer roles)

- Read-only product list with paging and sorting

### Mailbox

- In-app messaging
- Unread count badge
- Mark as read/unread
- Real-time updates

### i18n

- Hungarian and English translations
- Date localization sync
- Backend preference sync
- Language switcher in the shell

### Theme

- Light and dark mode
- Persistent user preference
- Material Design 3

## Development

### Prerequisites

- Node.js 22.22.3+, 24.15.0+ or 26.0.0+
- npm 10+

### Install dependencies

```bash
npm install
```

### Development server

```bash
npm start
```

The app runs at http://localhost:4200 and proxies to the backend gateway (http://localhost:8080).

**Proxy configuration:** [proxy.conf.json](proxy.conf.json)

### Build

```bash
npm run build
```

The production build goes to the `dist/` directory.

### Testing

```bash
npm run test
```

**Current state:** 88 Vitest unit tests in 21 test files (pass/fail status is shown by the CI `build-test.yml` run)

**Single run:**

```bash
npm run test:run
```

### Code quality

**Lint:**

```bash
npm run lint
npm run lint:fix
```

**Formatting:**

```bash
npm run format
npm run format:check
```

**Full style check:**

```bash
npm run style:check
```

## OpenAPI client generation

The frontend uses TypeScript clients generated from the backend OpenAPI specifications.

### Generate all clients

```bash
npm run generate:api
```

This runs:

1. `generate:types` - generates TypeScript types from `Api.json`
2. `generate:client` - generates Angular services from `Api.json`
3. `generate:products` - generates types and services from `ProductsApi.json`

### Manual steps

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

(Starts the backend services, downloads the specifications and generates the clients)

## Project structure

```
src/app/
├── core/                   <- Core services and guards
│   ├── auth/               <- Auth service, guards, interceptors
│   ├── i18n/               <- Language service
│   ├── mailbox/            <- Mailbox service
│   ├── models/             <- Shared models
│   ├── services/           <- Other core services
│   └── theme/              <- Theme service
├── features/               <- Feature modules
│   ├── admin/              <- Admin pages (users, facilities, audit-log)
│   ├── auth/               <- Auth pages (login, register, set-password)
│   ├── facility/           <- Facility pages (users, products)
│   ├── mailbox/            <- Mailbox page
│   └── products/           <- Products page (FacilityEditor/Viewer)
├── generated/              <- Generated OpenAPI clients
│   ├── client/             <- Core API client
│   ├── products-client/    <- Products API client
│   ├── api.types.ts        <- Core API types
│   └── products.api.types.ts  <- Products API types
├── shared/                 <- Shared components and utilities
│   ├── components/         <- Reusable components
│   ├── shell/              <- Shell layout with navigation
│   └── utils/              <- Utility functions, mappers
└── app.routes.ts           <- Application routing
```

## Routes

| Route                   | Component                     | Guard                                                | Description                                |
| ----------------------- | ----------------------------- | ---------------------------------------------------- | ------------------------------------------ |
| `/auth/login`           | AuthLoginComponent            | -                                                    | Login page                                 |
| `/auth/register`        | AuthRegisterComponent         | -                                                    | Registration page                          |
| `/auth/set-password`    | AuthSetPasswordComponent      | -                                                    | Password setup (from an invitation link)   |
| `/auth/forgot-password` | AuthForgotPasswordComponent   | -                                                    | Forgotten password - request a reset email |
| `/admin/users`          | AdminUsersPageComponent       | authGuard, roleGuard(SystemAdmin)                    | User management                            |
| `/admin/facilities`     | AdminFacilitiesPageComponent  | authGuard, roleGuard(SystemAdmin)                    | Facility management                        |
| `/admin/audit-log`      | AdminAuditLogPageComponent    | authGuard, roleGuard(SystemAdmin)                    | Audit log viewer                           |
| `/facility/users`       | FacilityUsersPageComponent    | authGuard, roleGuard(FacilityAdmin)                  | Facility user management                   |
| `/facility/products`    | FacilityProductsPageComponent | authGuard, roleGuard(FacilityAdmin)                  | Facility product management                |
| `/products`             | ProductsPageComponent         | authGuard, roleGuard(FacilityEditor, FacilityViewer) | Product list (read-only)                   |
| `/mailbox`              | MailboxPageComponent          | authGuard                                            | Mailbox messages                           |

**Route definitions:** [app.routes.ts](src/app/app.routes.ts)

## i18n (internationalization)

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

**Switching languages:**

- Users switch with the dropdown in the shell
- The preference is saved to the backend
- Date localization is synced automatically

## Theme system

**ThemeService:**

- Light/dark mode toggle
- Persistent preference (localStorage)
- Material Design 3 theming

**Usage:**

```typescript
this.themeService.toggleTheme();
this.themeService.isDark(); // Signal<boolean>
```

## Code style and Git hooks

**Configuration:**

- [.editorconfig](.editorconfig) - Editor settings
- [eslint.config.js](eslint.config.js) - ESLint rules
- [.prettierrc.json](.prettierrc.json) - Prettier formatting

**Pre-commit hook:**

- `lint-staged` runs on staged files
- ESLint + Prettier on `.ts` and `.html` files
- Prettier only on `.scss`, `.css`, `.json`, `.js`, `.md` files

**Setup:**

```bash
npm run prepare
```

## Testing strategy

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

**Running tests:**

```bash
npm run test          # Watch mode
npm run test:run      # Single run
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

The frontend is served by nginx, which reverse-proxies to the backend gateway.

**nginx configuration:** [nginx.conf](nginx.conf)

## Security headers (nginx)

- `Content-Security-Policy` - restricts resource loading
- `X-Frame-Options: DENY` - clickjacking prevention
- `X-Content-Type-Options: nosniff` - MIME sniffing prevention
- `Referrer-Policy: strict-origin-when-cross-origin` - referrer control
- `Permissions-Policy` - disables unneeded browser features

## Environment configuration

The frontend uses the Angular environment system.

**Development:**

- API proxy to `http://localhost:8080`
- Source maps enabled
- Development mode

**Production:**

- API base URL: `/api` (served through nginx)
- Optimized build
- AOT compilation

## NPM scripts

| Script                      | Purpose                                    |
| --------------------------- | ------------------------------------------ |
| `npm start`                 | Development server (http://localhost:4200) |
| `npm run build`             | Production build                           |
| `npm run watch`             | Watch mode build                           |
| `npm run test`              | Vitest watch mode                          |
| `npm run test:run`          | Vitest single run                          |
| `npm run lint`              | ESLint check                               |
| `npm run lint:fix`          | ESLint auto-fix                            |
| `npm run format`            | Prettier formatting                        |
| `npm run format:check`      | Prettier check                             |
| `npm run style:check`       | Full style gate (format + lint)            |
| `npm run generate:api`      | Generate all OpenAPI clients               |
| `npm run generate:types`    | Generate Core API types                    |
| `npm run generate:client`   | Generate Core API services                 |
| `npm run generate:products` | Generate the Products API client           |
| `npm run prepare`           | Set up Git hooks                           |

## Notes

- **Standalone components:** no NgModules, uses the Angular 22 standalone APIs
- **Signals:** state management with Angular Signals where applicable
- **Material Design 3:** latest Material components and theming
- **OpenAPI-first:** all API communication goes through generated clients
- **Type-safe:** full TypeScript coverage in strict mode
- **Test coverage:** 88 unit tests covering the core functionality
