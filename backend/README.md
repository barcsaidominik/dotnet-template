# Backend

.NET 10 backend services showcasing Clean Architecture, CQRS, microservices, and production-ready patterns.

## Tech Stack

| Component | Technology |
|-----------|-----------|
| **Framework** | .NET 10, ASP.NET Core |
| **Database** | PostgreSQL 17, EF Core, Npgsql |
| **Architecture** | Clean Architecture, CQRS |
| **Messaging** | Mediator (not MediatR) |
| **Error Handling** | ErrorOr |
| **Validation** | FluentValidation (pipeline behavior) |
| **Authentication** | ASP.NET Core Identity, JWT + Refresh Tokens |
| **Logging** | Serilog, Syslog RFC 5424 |
| **Background Jobs** | TickerQ (EF Core store) |
| **PDF Generation** | QuestPDF + Scriban templates |
| **Excel** | ClosedXML |
| **Inter-Service** | gRPC + YARP Reverse Proxy |
| **Resilience** | Polly (retry policies) |

## Project Structure

```
backend/src/
├── common/                        ← Cross-cutting concerns
│   ├── Template.Common/           ← Retry helpers, logging middleware
│   ├── Template.Common.Email/     ← SMTP email service (Polly retry)
│   ├── Template.Common.Excel/     ← ClosedXML wrapper
│   ├── Template.Common.Jobs/      ← TickerQ integration
│   ├── Template.Common.Pdf/       ← QuestPDF integration
│   └── Template.Common.Templating/← Scriban template engine
├── core/                          ← Clean Architecture layers
│   ├── Template.Domain/           ← Entities, enums, domain logic
│   ├── Template.Application/      ← Use cases, CQRS handlers, DTOs
│   ├── Template.Infrastructure/   ← EF Core, Identity, external integrations
│   └── Template.Grpc/             ← Shared proto definitions
└── services/                      ← API hosts
    ├── Template.Api/              ← Core API (auth, admin, users, mailbox)
    ├── Template.Products.Api/     ← Products microservice (REST + gRPC)
    └── Template.Gateway.Api/      ← YARP reverse proxy gateway

backend/tests/
└── Template.Tests/                ← 133 tests (unit + integration)
```

## Services

### Template.Api (Core API)

**Responsibilities:**
- Authentication & authorization (register, login, refresh, approve)
- User management (CRUD, role assignment)
- Facility management (CRUD, user assignment)
- Mailbox (in-app messaging)
- Background job scheduling (TickerQ)
- Admin endpoints (audit log, Excel exports)
- Middleware (telemetry, resource guard)

**Endpoints:**
- `/api/Auth` - Register, Login, Refresh, SetPassword
- `/api/Users` - Get current user, update preferences
- `/api/Admin` - User/facility CRUD, approvals, audit log, exports
- `/api/FacilityUsers` - Facility-scoped user management
- `/api/Mailbox` - Mailbox messages, unread count
- `/health/live`, `/health/ready`, `/health` - Health checks
- `/admin/tickerq` - TickerQ dashboard (SystemAdmin only)
- `/scalar/v1` - Scalar API documentation

### Template.Products.Api (Products Microservice)

**Responsibilities:**
- Product CRUD with pagination, sorting, search
- Excel import/export (localized)
- PDF order generation (QuestPDF + Scriban)
- gRPC endpoint for facility usage checks

**Endpoints:**
- `/api/Products` - CRUD, pagination, exports, imports, PDF generation
- gRPC: `FacilityProductsGrpcService` (port 8081)

**Deployment Modes:**
- **InProcess:** Products functionality runs inside Template.Api (monolithic)
- **Proxy:** Products.Api runs as separate service, Gateway routes requests

### Template.Gateway.Api (Reverse Proxy)

YARP-based gateway that routes requests to backend services.

**Routes:**
- `/api/auth/*`, `/api/users/*`, `/api/admin/*`, `/api/facilityusers/*`, `/api/mailbox/*` → Core API
- `/api/products/*` → Products API

## Authentication Flow

1. **Registration:**
   - User registers with email/password
   - `IsApproved = false` by default
   - Admin must approve and assign facility + role

2. **Approval:**
   - Admin approves user via `/api/Admin/users/{id}/approve`
   - Assigns facility and role (SystemAdmin, FacilityAdmin, FacilityEditor, FacilityViewer)

3. **Login:**
   - POST `/api/Auth/login` with credentials
   - Returns JWT (15-minute expiration) + refresh token (7-day expiration)
   - Refresh tokens stored as SHA-256 hashes
   - `ClockSkew = TimeSpan.Zero` (strict JWT expiration)

4. **Token Refresh:**
   - POST `/api/Auth/refresh` with refresh token
   - Returns new JWT + new refresh token

5. **Facility User Creation:**
   - Admin creates facility user with `RequiresPasswordChange = true`
   - Returns `setupToken` for `/api/Auth/set-password` flow

## Deployment Modes

### InProcess (Monolithic)

Products functionality runs inside `Template.Api`.

**Configuration:**
```json
{
  "ProductsService": {
    "Mode": "InProcess"
  }
}
```

### Proxy (Microservices)

Products runs as separate `Template.Products.Api` service.

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
- Gateway exposes port 80 to external clients
- Core API communicates with Products API via gRPC (internal network)
- Internal service authentication via `InternalServiceAuth__Token`

## Development

### Prerequisites
- .NET 10 SDK
- PostgreSQL 17
- Docker (optional, for containerized development)

### Local Development

1. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

2. **Apply migrations:**
   ```bash
   cd src/services/Template.Api
   dotnet ef database update
   ```

3. **Run Core API:**
   ```bash
   dotnet run --project src/services/Template.Api
   ```

4. **Run Products API (if using Proxy mode):**
   ```bash
   dotnet run --project src/services/Template.Products.Api
   ```

5. **Run Gateway (if using Proxy mode):**
   ```bash
   dotnet run --project src/services/Template.Gateway.Api
   ```

### Testing

```bash
dotnet test tests/Template.Tests/Template.Tests.csproj
```

**Current status:** 133/133 passing (unit + integration)

### OpenAPI Client Generation

OpenAPI specs are generated and used by the frontend:
- `Api.json` - Core API spec
- `ProductsApi.json` - Products API spec

Regenerate with:
```powershell
.\scripts\Refresh-OpenApi.ps1
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | - |
| `JwtSettings__Secret` | JWT signing secret (min 32 chars) | - |
| `JwtSettings__Issuer` | JWT issuer | `template-api` |
| `JwtSettings__Audience` | JWT audience | `template-frontend` |
| `InternalServiceAuth__Token` | Service-to-service auth token (min 32 chars) | - |
| `ProductsService__Mode` | Deployment mode: `InProcess` or `Proxy` | `InProcess` |
| `ProductsService__GrpcBaseUrl` | Products gRPC endpoint (Proxy mode) | - |
| `ASPNETCORE_ENVIRONMENT` | Environment: Development, Staging, Production | `Development` |

See [.env.example](../.env.example) for Docker Compose configuration.

## Security Features

### Authentication & Authorization
- SHA-256 hashed refresh tokens (no plain text storage)
- DataProtection keys persisted to `/app/keys` volume
- Rate limiting: 10 requests/min on auth endpoints (disabled in Development)
- Role-based authorization policies

### Service-to-Service Security
- Internal service authentication with token validation
- Constant-time token comparison (`CryptographicOperations.FixedTimeEquals`)
- Minimum 32-character token length enforced

### Data Protection
- BOLA protection with `QueryGuard<T>` (prevents unauthorized facility access)
- Query string redaction (passwords, tokens, refresh tokens, access tokens)
- Request telemetry with CPU/RAM monitoring
- Resource guard middleware (cooperative cancellation under load)

### Audit
- Entity change tracking via `AuditInterceptor` (singleton, scoped to Facility + Product entities)
- Audit log with filtering: entity type, action (Created/Updated/Deleted), user, date range
- N+1 query prevention (batch queries, `ExecuteUpdateAsync`)

## Middleware Pipeline

```
RequestTelemetryMiddleware
↓
ResourceGuardMiddleware
↓
RateLimiter (non-Development only)
↓
Authentication
↓
Authorization
↓
TickerQ
↓
Controllers
```

## Background Jobs (TickerQ)

**Dashboard:** `/admin/tickerq` (SystemAdmin role required)

**Examples:**
- `DemoLongRunningOperation` - Manual trigger demo
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
8. HashRefreshToken (migrates plain text tokens to SHA-256 hashes)

**Auto-migration:** Enabled in Development mode on startup

**Seeding:**
- Roles: SystemAdmin, FacilityAdmin, FacilityEditor, FacilityViewer
- Default admin: `admin@example.com` / `Admin123!`

## Caching

**GetProducts:**
- In-memory cache per facility (`products:facility:{id}`)
- 5-minute TTL
- Invalidation: Create/Update product
- Sort/search/pagination performed in-memory after cache retrieval

**GetAllUsers:**
- In-memory cache (5-minute TTL)
- Invalidation: Create/Approve/Delete/UpdateRole

## Health Checks

| Endpoint | Purpose |
|----------|---------|
| `/health/live` | Liveness probe (always returns 200) |
| `/health/ready` | Readiness probe (checks database connection) |
| `/health` | Combined health status |

## Resilience (Polly)

**SMTP Email Service:**
- 3 retries with 2-second delay
- Skips retry for authentication errors

**gRPC Facility Usage Service:**
- 3 retries with exponential backoff (1s base)
- Skips retry for invalid argument errors

## Docker

**Images:**
- `Dockerfile` - Core API
- `Dockerfile.ProductsApi` - Products API
- `Dockerfile.GatewayApi` - Gateway API

**Build from repository root:**
```bash
docker compose up -d --build
```

**Volumes:**
- `dataprotection-keys` - Shared DataProtection keys across containers
- `C:\DB\postgres-data` - PostgreSQL data persistence

**Secrets:**
- `secrets/jwt_secret.txt`
- `secrets/internal_service_token.txt`

## API Documentation

**Scalar UI:** http://localhost:8080/scalar/v1

Includes Bearer token authentication scheme for testing authenticated endpoints.

## Notes

- **Mediator:** Uses `Mediator` NuGet package (`using Mediator;`), not MediatR
- **ErrorOr:** Functional error handling pattern (no exceptions for business logic errors)
- **Entity base class:** `Domain/Common/Entity.cs` with `Guid Id` and `DateTime CreatedAt`
- **QuestPDF:** Community license (free for open-source/evaluation, purchase required for commercial use)
