# .NET Template Project

A production-ready full-stack template demonstrating modern .NET and Angular development patterns with real-world features.

This is a **demonstration template**, not a business application. Each feature showcases enterprise-grade implementation patterns including authentication, authorization, background jobs, PDF/Excel generation, microservices, gRPC, and more.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | .NET 10, ASP.NET Core Web API |
| **Frontend** | Angular 20, Standalone Components, Signals |
| **Architecture** | Clean Architecture, CQRS (Mediator) |
| **Database** | PostgreSQL 17, EF Core |
| **Authentication** | ASP.NET Core Identity, JWT + Refresh Tokens |
| **Background Jobs** | TickerQ (EF Core store) |
| **Document Generation** | QuestPDF (PDF), ClosedXML (Excel) |
| **Communication** | gRPC, YARP Reverse Proxy |
| **Resilience** | Polly (retry policies) |

## Quick Start

1. **Clone and configure environment:**
   ```bash
   git clone <repository-url>
   cd dotnet-template
   cp .env.example .env
   ```

2. **Create Docker secrets:**
   ```bash
   mkdir -p secrets
   echo "your-jwt-secret-at-least-32-characters-long" > secrets/jwt_secret.txt
   echo "your-internal-service-token-here" > secrets/internal_service_token.txt
   ```

3. **Start all services:**
   ```bash
   docker compose up -d --build
   ```

4. **Access the application:**
   - Frontend: http://localhost
   - API Documentation (Scalar): http://localhost:8080/scalar/v1
   - TickerQ Dashboard: http://localhost:8080/admin/tickerq (SystemAdmin role required)

## Features

### Authentication & Authorization
- **Registration flow:** Register → Admin approval → Role assignment
- **JWT authentication:** 15-minute access tokens with 7-day refresh tokens
- **Role-based access:** SystemAdmin, FacilityAdmin, FacilityEditor, FacilityViewer
- **Secure token storage:** SHA-256 hashed refresh tokens, DataProtection key persistence

### User Management
- Admin user approval workflow
- Facility-based user organization
- Role management and updates
- Excel export with localization

### Facilities & Products
- Multi-tenant facility structure
- Product CRUD with pagination, sorting, and search
- Excel import/export (localized)
- PDF order generation (QuestPDF + Scriban templates)
- In-memory caching with smart invalidation

### Microservices & Communication
- **InProcess mode:** Monolithic deployment
- **Proxy mode:** Gateway API with YARP routing to separate services
- **gRPC:** Inter-service communication with Polly retry policies
- **Internal service authentication:** Token-based service-to-service auth

### Background Jobs
- TickerQ integration with EF Core storage
- Dashboard UI for job monitoring
- Cron-based scheduling demo
- Long-running operation example

### Audit & Monitoring
- Entity change tracking (Facilities, Products)
- Audit log with filtering (entity type, action, date range, user)
- Serilog with Syslog RFC 5424 format
- Health checks (liveness, readiness)
- Request telemetry (CPU, RAM, query redaction)

### Mailbox
- In-app messaging system
- Unread count tracking
- Real-time updates

### Security
- Rate limiting (10 req/min on auth endpoints)
- BOLA protection with QueryGuard
- Constant-time token comparison (gRPC)
- Query string redaction (passwords, tokens)
- CSP, X-Frame-Options, Referrer-Policy headers

### Developer Experience
- Clean Architecture with clear separation
- ErrorOr result pattern
- FluentValidation with pipeline behavior
- OpenAPI/Scalar documentation
- Hot reload support
- 133 backend tests (unit + integration)
- 88 frontend Vitest tests

## Project Structure

```
dotnet-template/
├── backend/              ← .NET 10 backend services
├── frontend/             ← Angular 20 frontend
├── docker-compose.yml    ← Multi-container orchestration
├── .env.example          ← Environment variable template
└── secrets/              ← Docker secrets (create manually)
```

## Documentation

- [Backend README](backend/README.md) - Architecture, deployment modes, API details
- [Frontend README](frontend/README.md) - Angular setup, i18n, testing

## Default Credentials

After first run, a system admin is seeded:
- **Email:** `admin@example.com`
- **Password:** `Admin123!`

**Change this immediately in production.**

## License

This template is for demonstration purposes. Adjust licensing for your use case.

## Notes

- **QuestPDF:** Community license (free for open-source and evaluation). Purchase required for commercial use.
- **Data Protection Keys:** Persisted to `/app/keys` volume for token consistency across restarts
- **Database Migrations:** Auto-applied on startup in Development mode
