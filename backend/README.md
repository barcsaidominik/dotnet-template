# Backend

**English** | [Magyar](README.hu.md)

.NET 10 backend services demonstrating Clean Architecture, CQRS, microservices and production-oriented patterns.

## Tech Stack

| Component | Technology |
|-----------|------------|
| **Framework** | .NET 10, ASP.NET Core |
| **Database** | PostgreSQL 17, EF Core, Npgsql |
| **Architecture** | Clean Architecture, CQRS |
| **Messaging** | Mediator (not MediatR) |
| **Error handling** | ErrorOr |
| **Validation** | FluentValidation (pipeline behavior) |
| **Authentication** | ASP.NET Core Identity, JWT + Refresh Tokens |
| **Logging** | Serilog, Syslog RFC 5424, Elastic.Serilog.Sinks (Template.Api) |
| **Background jobs** | TickerQ (EF Core store) |
| **PDF generation** | QuestPDF + Scriban templates |
| **Excel** | ClosedXML |
| **Service-to-service** | gRPC + YARP Reverse Proxy |
| **Resilience** | Polly (retry policies) |

## Project structure

```
backend/src/
├── common/                        <- Cross-cutting concerns
│   ├── Template.Common/           <- Retry helpers, logging middleware, idempotency
│   ├── Template.Common.Email/     <- SMTP email service (Polly retry)
│   ├── Template.Common.Excel/     <- ClosedXML wrapper
│   ├── Template.Common.Jobs/      <- TickerQ integration
│   ├── Template.Common.Pdf/       <- QuestPDF integration
│   └── Template.Common.Templating/<- Scriban template engine
├── core/                          <- Clean Architecture layers
│   ├── Template.Domain/           <- Entities, enums, domain logic
│   ├── Template.Application/      <- Use cases, CQRS handlers, DTOs
│   ├── Template.Infrastructure/   <- EF Core, Identity, external integrations
│   └── Template.Grpc/             <- Shared proto definitions
└── services/                      <- API hosts
    ├── Template.Api/              <- Core API (auth, admin, users, mailbox)
    ├── Template.Products.Api/     <- Products microservice (REST + gRPC)
    ├── Template.Gateway.Api/      <- YARP reverse proxy gateway
    └── Template.Migrator/         <- One-shot EF Core migration console app

backend/tests/
└── Template.Tests/                <- 154 tests (unit + integration)
```

## Services

### Template.Api (Core API)

**Responsibilities:**
- Authentication and authorization (register, login, refresh, approve)
- User management (CRUD, role assignment, email change)
- Facility management (CRUD, user assignment)
- Mailbox (in-app messaging)
- Background job scheduling (TickerQ)
- Admin endpoints (audit log, Excel exports)
- Middleware (telemetry, resource guard, idempotency)

**Endpoints:**
- `/api/Auth` - Register, Login, Refresh, SetPassword
- `/api/Users` - Current user, preference update
- `/api/Admin` - User/facility CRUD, approval, audit log, exports
- `/api/Admin/users/{userId}` - PUT email change
- `/api/FacilityUsers` - Facility-level user management
- `/api/Mailbox` - Mailbox messages, unread count
- `/health/live`, `/health/ready`, `/health` - Health checks
- `/admin/tickerq` - TickerQ dashboard (SystemAdmin only)
- `/scalar/v1` - Scalar API reference

### Template.Products.Api (Products Microservice)

**Responsibilities:**
- Product CRUD with paging, sorting and search
- Excel import/export (localized)
- PDF order generation (QuestPDF + Scriban)
- gRPC endpoint for facility usage checks

**Endpoints:**
- `/api/Products` - CRUD, paging, exports, imports, PDF generation
- gRPC: `FacilityProductsGrpcService` (port 8081)

### Template.Gateway.Api (Reverse Proxy)

YARP-based gateway that routes requests to the backend services.

**Routes:**
- `/api/products/*` -> Products API
- `/{**catch-all}` -> Core API (everything else)

## Authentication flow

1. **Registration:**
   - The user registers with email/password
   - `IsApproved = false` by default
   - An admin has to approve the user and assign a facility and a role

2. **Approval:**
   - The admin approves the user on the `/api/Admin/users/{id}/approve` endpoint
   - Assigns a facility and a role (SystemAdmin, FacilityAdmin, FacilityEditor, FacilityViewer)

3. **Login:**
   - POST `/api/Auth/login` with credentials
   - Returns a JWT (15-minute lifetime) + a refresh token (7-day lifetime)
   - Refresh tokens are stored as SHA-256 hashes
   - `ClockSkew = TimeSpan.Zero` (strict JWT expiry)

4. **Token refresh:**
   - POST `/api/Auth/refresh` with the refresh token
   - Returns a new JWT + a new refresh token

5. **Facility user creation:**
   - An admin creates a facility user with `RequiresPasswordChange = true`
   - Returns a `setupToken` for the `/api/Auth/set-password` flow

## Deployment mode

### Proxy (microservices)

Products run as a separate `Template.Products.Api` service.

**Configuration:**
```json
{
  "ProductsService": {
    "Mode": "Proxy",
    "GrpcBaseUrl": "http://products-api:8081"
  }
}
```

**Docker Compose:**
- The gateway publishes port 8080 to external clients (the frontend nginx publishes port 80)
- The Core API talks to the Products API over gRPC (internal network)
- Internal service authentication with `InternalServiceAuth__Token`

## Development

### Prerequisites
- .NET 10 SDK
- PostgreSQL 17
- Docker (optional, for containerized development)

### Local development

1. **Restore dependencies:**
   ```bash
   dotnet restore
   dotnet tool restore
   ```

2. **Apply migrations** (`Template.Migrator` reads the connection string from environment variables only; `Template.Api` no longer migrates on startup and exits on an unmigrated schema):
   ```bash
   ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=template_db;Username=postgres;Password=your-db-password" \
     dotnet run --project src/services/Template.Migrator
   ```
   PowerShell:
   ```powershell
   $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=template_db;Username=postgres;Password=your-db-password"
   dotnet run --project src/services/Template.Migrator
   ```

3. **Start the Core API:**
   ```bash
   dotnet run --project src/services/Template.Api
   ```

4. **Start the Products API (Proxy mode):**
   ```bash
   dotnet run --project src/services/Template.Products.Api
   ```

5. **Start the gateway (Proxy mode):**
   ```bash
   dotnet run --project src/services/Template.Gateway.Api
   ```

### Testing

```bash
dotnet test tests/Template.Tests/Template.Tests.csproj
```

**Current state:** 154 tests (unit + integration; pass/fail status is shown by the CI `build-test.yml` run)

### OpenAPI client generation

OpenAPI specifications used by the frontend:
- `Api.json` - Core API spec
- `ProductsApi.json` - Products API spec

Regenerate:
```powershell
.\scripts\Refresh-OpenApi.ps1
```

## Environment variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | - |
| `JwtSettings__Secret` | JWT signing secret (min. 32 characters) | - |
| `JwtSettings__Issuer` | JWT issuer | `template-api` |
| `JwtSettings__Audience` | JWT audience | `template-frontend` |
| `InternalServiceAuth__Token` | Service-to-service auth token (min. 32 characters) | - |
| `ProductsService__GrpcBaseUrl` | Products gRPC endpoint (Proxy mode) | - |
| `ASPNETCORE_ENVIRONMENT` | Environment: Development, Staging, Production | `Development` |
| `SystemAdmin__Email` | System admin email seeded on first start | `admin@template.io` |

See [.env.example](../.env.example) for the Docker Compose configuration.

## Docker secrets

Docker secrets take precedence over environment variables when both exist.

**Supported secrets:**
- `jwt_secret` -> `JwtSettings:Secret`
- `internal_service_token` -> `InternalServiceAuth:Token`

The database connection string does not come from a Docker secret; it is read from `ConnectionStrings:DefaultConnection` (environment variable or user-secrets).

**Usage:**
```yaml
secrets:
  jwt_secret:
    file: ./secrets/jwt_secret.txt
  internal_service_token:
    file: ./secrets/internal_service_token.txt
```

The `AddDockerSecrets()` extension loads them from `/run/secrets/` automatically.

## Security features

### Authentication and authorization
- SHA-256 hashed refresh tokens (no plaintext storage)
- DataProtection keys persisted to the `/app/keys` volume
- Rate limiting: 10 requests/minute on auth endpoints (disabled in Development)
- Role-based authorization policies

### Service-to-service security
- Internal service authentication with token validation
- Constant-time token comparison (`CryptographicOperations.FixedTimeEquals`)
- Minimum token length of 32 characters

### Data protection
- BOLA protection with `QueryGuard<T>` (prevents access outside the user's facility)
- Query string redaction (passwords, tokens, refresh tokens, access tokens)
- Request telemetry with CPU/RAM monitoring
- Resource guard middleware (cooperative cancellation under load)

### Audit
- Entity change tracking with `AuditInterceptor` (singleton, for Facility + Product entities)
- Audit log with filters: entity type, action (Created/Updated/Deleted), user, date range
- N+1 query prevention (batched queries, `ExecuteUpdateAsync`)

## Optimistic concurrency

Optimistic concurrency is built on the PostgreSQL `xmin` system column.

**How it works:**
1. Every entity (Entity base class) has a `RowVersion` property (`uint`)
2. EF Core configuration: `.HasColumnName("xmin").HasColumnType("xid").IsRowVersion()`
3. On update, the client sends the `RowVersion` it read earlier
4. If the database holds a different value (someone else changed it in the meantime), a `DbUpdateConcurrencyException` is thrown
5. The handler returns 409 Conflict as `ProductErrors.ConcurrencyConflict` or a similar error

**Affected entities:** Facility, Product, MailboxMessage

## Idempotency (IdempotencyMiddleware)

POST requests can be retried safely with the `Idempotency-Key` header.

**How it works:**
1. The client sends an `Idempotency-Key` header (unique identifier)
2. The middleware checks the in-memory cache
3. Existing key: returns the cached response with an `X-Idempotency-Replayed: true` header
4. New key: executes the request and caches the response for 24 hours

**Excluded paths:** `/health`, `/scalar`, `/openapi`

**Note:** In-memory implementation, suitable for a single node. For multi-instance deployments use Redis or another distributed cache.

## Circuit breaker (ResourceGuardMiddleware)

Automatic load protection based on CPU and RAM thresholds.

**States:**
- **Closed:** Normal operation, requests are processed
- **Open:** Threshold exceeded, every request gets 503 with a `Retry-After` header
- **HalfOpen:** A probe request is let through; on success back to Closed, otherwise back to Open

**Configuration (ResourceGuardOptions):**
| Setting | Default | Description |
|---------|---------|-------------|
| `CpuThresholdPercent` | 90 | CPU threshold for the Open state |
| `MemoryThresholdPercent` | 95 | RAM threshold for the Open state |
| `CircuitBreakerDurationSeconds` | 30 | Duration of the Open state |
| `MaxWorkingSetMb` | 1024 | Max working set in MB |
| `MaxConcurrentRequests` | 200 | Max concurrent requests |
| `MinAvailableWorkerThreads` | 16 | Min available worker threads |

## Middleware pipeline

```
RequestTelemetryMiddleware
↓
ResourceGuardMiddleware
↓
RateLimiter (non-Development)
↓
Authentication
↓
Authorization
↓
IdempotencyMiddleware
↓
TickerQ
↓
Controllers
```

## Background jobs (TickerQ)

**Dashboard:** `/admin/tickerq` (requires the SystemAdmin role)

**Examples:**
- `DemoLongRunningOperation` - Manually triggered demo
- `Demo.Heartbeat` - Cron-based scheduled job

**Configuration:**
```csharp
builder.Services.AddBackgroundJobs<AppDbContext>(
    dashboardBasePath: "/admin/tickerq",
    dashboardPolicyName: "TickerQDashboard"
);
```

## Database

**Migrations:**
1. Init
2. AddRefreshToken
3. AddPreferredLanguage
4. AddTickerQBackgroundJobs
5. AddMailboxMessages
6. AddProductQuantity
7. AddAuditLog
8. HashRefreshToken (migrates plaintext tokens to SHA-256 hashes)
9. AddOptimisticConcurrency (EF Core mapping of the PostgreSQL xmin column)
10. AddTickerQIsSystemPaused (TickerQ `CronTickers.IsSystemPaused` column)

**Migration:** applied only by `Template.Migrator` (the `db-migrator` container under Docker Compose). On startup `Template.Api` only seeds (roles, system admin); on an unmigrated schema the seeding throws and the application exits.

**Seeding:**
- Roles: SystemAdmin, FacilityAdmin, FacilityEditor, FacilityViewer
- Default admin: `admin@template.io` (configurable via `SystemAdmin:Email`) - a password-setup email is sent on first start

## Cache

**GetProducts:**
- In-memory cache per facility (`products:facility:{id}`)
- 5-minute TTL
- Invalidation: Create/Update product
- Sorting/search/paging in memory after the cache read

**GetAllUsers:**
- In-memory cache (5-minute TTL)
- Invalidation: Create/Approve/Delete/UpdateRole

## Health checks

| Endpoint | Purpose |
|----------|---------|
| `/health/live` | Liveness probe (always returns 200) |
| `/health/ready` | Readiness probe (checks the database connection) |
| `/health` | Combined health status |

## Resilience (Polly)

**SMTP email service:**
- 3 retries with a 2-second delay
- No retry on authentication errors

**gRPC facility usage service:**
- 3 retries with exponential backoff (1s base)
- No retry on invalid argument errors

## Docker

**Images:**
- `Dockerfile` - Core API
- `Dockerfile.ProductsApi` - Products API
- `Dockerfile.GatewayApi` - Gateway API
- `Dockerfile.Migrator` - Migrator

**Build from the repository root:**
```bash
docker compose up -d --build
```

**Volumes:**
- `dataprotection-keys` - DataProtection keys shared between containers
- `postgres-data` - PostgreSQL data persistence (named volume; `docker compose down -v` resets it)

**Secrets:**
- `secrets/jwt_secret.txt`
- `secrets/internal_service_token.txt`

## API reference

**Scalar UI:** http://localhost:8080/scalar/v1

Bearer token authentication scheme for testing authenticated endpoints.

## Notes

- **Mediator:** uses the `Mediator` NuGet package (`using Mediator;`), not MediatR
- **ErrorOr:** functional error handling pattern (no exceptions for business logic errors)
- **Entity base class:** `Domain/Common/Entity.cs` with `Guid Id`, `DateTime CreatedAt` and `uint RowVersion`
- **QuestPDF:** Community license (free for open source/evaluation; commercial use above the revenue threshold requires a paid license)
