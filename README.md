# .NET Template

**English** | [Magyar](README.hu.md)

A full-stack reference template that demonstrates security-focused .NET 10 and Angular patterns with working features: identity and role-based access, tenant-scoped data access, CQRS with validation, gRPC service-to-service calls behind a YARP gateway, background jobs, PDF/Excel generation and audit logging. It is a demonstration codebase, not a business application, and the bundled Docker Compose setup targets local development, not production.

## Architecture

```
                 ┌──────────────┐
  browser ──:80─>│ frontend     │ Angular, served by nginx (CSP, X-Frame-Options)
                 └──────┬───────┘
                        │ /api
                 ┌──────v───────┐
         :8080 ─>│ api (gateway)│ YARP reverse proxy, CORS
                 └──┬────────┬──┘
     /api/products/*│        │ everything else
          ┌─────────v──┐  ┌──v─────────┐
          │products-api│<─┤ core-api   │ auth, admin, users, facilities, jobs
          │ REST + gRPC│  │            │ gRPC client (internal service token)
          └─────┬──────┘  └──┬─────────┘
                └─────┬──────┘
               ┌──────v──────┐   ┌─────────────┐
               │ PostgreSQL  │<──┤ db-migrator │ one-shot EF Core migration job
               └─────────────┘   └─────────────┘
```

Backend layout (`backend/src`):

| Folder | Projects |
|---|---|
| `services/` | `Template.Api` (core API), `Template.Products.Api` (products, gRPC server), `Template.Gateway.Api` (YARP), `Template.Migrator` |
| `core/` | `Template.Application` (CQRS commands/queries, FluentValidation), `Template.Domain`, `Template.Infrastructure` (EF Core, Identity, auth, query guards), `Template.Grpc` |
| `common/` | middleware, logging, configuration extensions, Email, Excel, Jobs, Pdf, Templating |

More detail: [backend/README.md](backend/README.md), [frontend/README.md](frontend/README.md).

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API |
| Frontend | Angular 22 (standalone components, signals), Angular Material, Vitest |
| Architecture | Clean Architecture, CQRS via `Mediator` (source-generated, not MediatR), ErrorOr results |
| Database | PostgreSQL 17, EF Core (Npgsql) |
| Auth | ASP.NET Core Identity, JWT access tokens + rotated refresh tokens |
| Background jobs | TickerQ (EF Core store, dashboard) |
| Documents | QuestPDF, ClosedXML, Scriban templates |
| Communication | gRPC with Polly retries, YARP reverse proxy |
| Logging | Serilog (RFC 5424 syslog format) |

## Getting started

Prerequisites: Docker with Compose v2. For running outside containers: .NET 10 SDK, Node.js 22, PostgreSQL 17.

1. **Clone and create the environment file**
   ```bash
   git clone https://github.com/barcsaidominik/dotnet-template.git
   cd dotnet-template
   cp .env.example .env
   ```
   Change the database password in `.env` if the port is reachable from other machines.

2. **Create the Docker secrets** (random values, at least 32 characters each)
   ```bash
   openssl rand -base64 48 > secrets/jwt_secret.txt
   openssl rand -base64 48 > secrets/internal_service_token.txt
   ```
   PowerShell:
   ```powershell
   [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48)) | Set-Content -NoNewline secrets/jwt_secret.txt
   [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48)) | Set-Content -NoNewline secrets/internal_service_token.txt
   ```
   Both files must exist before `docker compose up`. If an earlier run created *directories* with these names, delete them and create the files.

3. **Start everything**
   ```bash
   docker compose up -d --build
   ```
   Startup order: `db` (health-checked) → `db-migrator` (applies EF Core migrations, exits 0) → `core-api` and `products-api` (wait for `service_completed_successfully`) → gateway → frontend.

4. **Open**
   - Frontend: http://localhost
   - API reference (Scalar): http://localhost:8080/scalar/v1
   - TickerQ dashboard: http://localhost:8080/admin/tickerq (requires the `SystemAdmin` role)

**First login.** On first start a system admin is seeded with the address in `SystemAdmin:Email` (default `admin@template.io`). The account has no password; the app sends a password-setup email and the password is set on `/auth/set-password`. In Development the email service writes messages to files instead of sending them. Set `SystemAdmin__Email` to a real mailbox elsewhere.

For running the services with `dotnet run` and .NET user-secrets, see [backend/SECRETS_SETUP.md](backend/SECRETS_SETUP.md) and [backend/README.md](backend/README.md).

### Database migrations

- `Template.Migrator` is the only component that applies EF Core migrations. In Compose it runs as the one-shot `db-migrator` container.
- `Template.Api` only seeds roles and the system admin on startup. Against a missing or unmigrated schema the seeding throws and the service exits.
- `Template.Products.Api` neither migrates nor seeds.
- Outside Compose, run the migrator before starting the APIs:
  ```bash
  ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=template_db;Username=postgres;Password=your-db-password" \
    dotnet run --project backend/src/services/Template.Migrator
  ```

## Security notes

Implemented in code:

- JWT validation of issuer, audience, lifetime and signing key with `ClockSkew = 0`; the signing secret and the internal service token are validated on startup (`ValidateOnStart`, minimum 32 characters), so a missing secret stops the service.
- Refresh tokens are stored as SHA-256 hashes and rotated.
- Tenant isolation through query guards (for example `FacilityProductGuard` returns no rows when the user has no facility).
- Service-to-service gRPC calls use a separate internal token compared in constant time.
- Secrets can come from Docker secrets (`/run/secrets/jwt_secret`, `/run/secrets/internal_service_token`), which take precedence over environment variables. The database connection string is not read from a Docker secret; it comes from `ConnectionStrings:DefaultConnection`, and the services refuse to start when it is empty.
- Request telemetry redacts passwords and tokens from query strings; EF Core audit interceptor records before/after values.
- nginx sends CSP, `X-Frame-Options: DENY` and `Referrer-Policy` headers.

Local development defaults (do not reuse in production):

- Compose runs every service with `ASPNETCORE_ENVIRONMENT=Development`. In that mode the auth rate limiter is not registered and the refresh-token cookie is not marked `Secure`.
- PostgreSQL is published on host port 5432 with `postgres`/`postgres` unless `.env` overrides it.
- Scalar and the OpenAPI document are mapped in every environment.
- `appsettings.Development.json` contains a fixed development internal service token.
- There is no TLS termination in the Compose setup.

For production: run with `ASPNETCORE_ENVIRONMENT=Production`, supply all secrets from a secret store, do not publish the database port, terminate TLS in front of the gateway, and restrict or remove the Scalar endpoint.

**Thesis sample PRs.** Closed, unmerged pull requests (for example #41) contain deliberately vulnerable code and made-up secrets used in a measurement experiment for an MSc thesis. They are not part of `master`.

## Validation

| Check | Where | Command |
|---|---|---|
| Backend build (`-warnaserror`) + tests (154, xUnit v3, unit + `WebApplicationFactory` integration tests on EF InMemory) | `build-test.yml` | `dotnet test --project backend/tests/Template.Tests/Template.Tests.csproj` |
| Frontend ESLint, build, tests (88 Vitest tests) | `build-test.yml` | `cd frontend && npm ci && npm run lint && npm run build && npm run test:run` |
| Backend formatting | `style-check.yml` | `dotnet format backend/Template.slnx --verify-no-changes --severity error` |
| Backend Docker image builds (4 images; the frontend image is not built in CI) | `docker-build.yml` | `docker compose build` |
| CodeQL (with a Mediator taint extension), NuGet vulnerability audit, Gitleaks | `security-scanning.yml` | — |

The test counts are from a local run on the current `master`; CI is the source of truth for pass/fail status.

## Known limitations

- The Compose setup is for local development only (see Security notes).
- Integration tests use the EF Core InMemory provider, so PostgreSQL-specific behavior (migrations, `xmin` concurrency tokens) is not covered by tests.
- Idempotency responses are cached in memory, so they are not shared across instances.
- QuestPDF is used under its Community license, which has revenue limits for commercial use.

## License

[MIT](LICENSE). Third-party dependencies keep their own licenses; in particular QuestPDF is used under its Community license, which restricts commercial use above its revenue threshold.
