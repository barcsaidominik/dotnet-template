# Backend

.NET 10 backend szolgáltatások, Clean Architecture, CQRS, microservice-ek és production-ready minták bemutatása.

## Tech Stack

| Komponens | Technológia |
|-----------|-------------|
| **Framework** | .NET 10, ASP.NET Core |
| **Adatbázis** | PostgreSQL 17, EF Core, Npgsql |
| **Architektúra** | Clean Architecture, CQRS |
| **Messaging** | Mediator (nem MediatR) |
| **Hibakezelés** | ErrorOr |
| **Validáció** | FluentValidation (pipeline behavior) |
| **Hitelesítés** | ASP.NET Core Identity, JWT + Refresh Tokens |
| **Logging** | Serilog, Syslog RFC 5424, Elastic.Serilog.Sinks (Template.Api) |
| **Háttérfeladatok** | TickerQ (EF Core store) |
| **PDF generálás** | QuestPDF + Scriban sablonok |
| **Excel** | ClosedXML |
| **Szolgáltatás-közi** | gRPC + YARP Reverse Proxy |
| **Rugalmasság** | Polly (retry policy-k) |

## Projekt struktúra

```
backend/src/
├── common/                        <- Cross-cutting concerns
│   ├── Template.Common/           <- Retry helperek, logging middleware, idempotencia
│   ├── Template.Common.Email/     <- SMTP email szolgáltatás (Polly retry)
│   ├── Template.Common.Excel/     <- ClosedXML wrapper
│   ├── Template.Common.Jobs/      <- TickerQ integráció
│   ├── Template.Common.Pdf/       <- QuestPDF integráció
│   └── Template.Common.Templating/<- Scriban template engine
├── core/                          <- Clean Architecture rétegek
│   ├── Template.Domain/           <- Entitások, enumok, domain logika
│   ├── Template.Application/      <- Use case-ek, CQRS handlerek, DTO-k
│   ├── Template.Infrastructure/   <- EF Core, Identity, külső integrációk
│   └── Template.Grpc/             <- Megosztott proto definíciók
└── services/                      <- API hostok
    ├── Template.Api/              <- Core API (auth, admin, users, mailbox)
    ├── Template.Products.Api/     <- Products microservice (REST + gRPC)
    └── Template.Gateway.Api/      <- YARP reverse proxy gateway

backend/tests/
└── Template.Tests/                <- 133 teszt (unit + integration)
```

## Szolgáltatások

### Template.Api (Core API)

**Felelősségek:**
- Hitelesítés és jogosultságkezelés (register, login, refresh, approve)
- Felhasználókezelés (CRUD, szerepkör hozzárendelés, email módosítás)
- Létesítmény kezelés (CRUD, felhasználó hozzárendelés)
- Postafiók (alkalmazáson belüli üzenetküldés)
- Háttérfeladat ütemezés (TickerQ)
- Admin endpointok (audit log, Excel exportok)
- Middleware (telemetria, resource guard, idempotencia)

**Endpointok:**
- `/api/Auth` - Register, Login, Refresh, SetPassword
- `/api/Users` - Aktuális felhasználó, preferencia frissítés
- `/api/Admin` - Felhasználó/létesítmény CRUD, jóváhagy, audit log, exportok
- `/api/Admin/users/{userId}` - PUT email módosítás
- `/api/FacilityUsers` - Létesítmény-szintű felhasználókezelés
- `/api/Mailbox` - Postafiók üzenetek, olvasatlan darabszám
- `/health/live`, `/health/ready`, `/health` - Health checkek
- `/admin/tickerq` - TickerQ dashboard (csak SystemAdmin)
- `/scalar/v1` - Scalar API dokumentáció

### Template.Products.Api (Products Microservice)

**Felelősségek:**
- Termék CRUD lapozással, rendezéssel, kereséssel
- Excel import/export (lokalizált)
- PDF rendelés generálás (QuestPDF + Scriban)
- gRPC endpoint létesítmény használat ellenőrzéshez

**Endpointok:**
- `/api/Products` - CRUD, lapozás, exportok, importok, PDF generálás
- gRPC: `FacilityProductsGrpcService` (port 8081)

### Template.Gateway.Api (Reverse Proxy)

YARP-alapú gateway ami kéréseket irányít a backend szolgáltatásokhoz.

**Útvonalak:**
- `/api/products/*` -> Products API
- `/{**catch-all}` -> Core API (minden más)

## Hitelesítési folyamat

1. **Regisztráció:**
   - Felhasználó regisztrál email/jelszóval
   - `IsApproved = false` alapértelmezetten
   - Adminnak kell jóváhagy és létesítményt + szerepkört rendelni

2. **Jóváhagyás:**
   - Admin jóváhagy felhasználót a `/api/Admin/users/{id}/approve` endpointon
   - Hozzárendel létesítményt és szerepkört (SystemAdmin, FacilityAdmin, FacilityEditor, FacilityViewer)

3. **Bejelentkezés:**
   - POST `/api/Auth/login` hitelesítő adatokkal
   - Visszaad JWT-t (15 perces lejárás) + refresh tokent (7 napos lejárás)
   - Refresh tokenek SHA-256 hash-kent tarolva
   - `ClockSkew = TimeSpan.Zero` (szigorú JWT lejárás)

4. **Token frissítés:**
   - POST `/api/Auth/refresh` refresh tokennel
   - Visszaad új JWT-t + új refresh tokent

5. **Létesítmény felhasználó létrehozás:**
   - Admin létrehoz létesítmény felhasználót `RequiresPasswordChange = true`-val
   - Visszaad `setupToken`-t a `/api/Auth/set-password` folyamathoz

## Deployment mod

### Proxy (Microservice-ek)

Products külön `Template.Products.Api` szolgáltatás.

**Konfiguráció:**
```json
{
  "ProductsService": {
    "Mode": "Proxy",
    "GrpcBaseUrl": "http://products-api:8081"
  }
}
```

**Docker Compose:**
- Gateway 80-as portot expoze-ol külső klienseknek
- Core API gRPC-n kommunikál a Products API-val (belső hálózat)
- Belső szolgáltatás hitelesítés `InternalServiceAuth__Token`-nel

## Fejlesztés

### Előfeltételek
- .NET 10 SDK
- PostgreSQL 17
- Docker (opcionális, konténeres fejlesztéshez)

### Lokális fejlesztés

1. **Függőségek visszaállítása:**
   ```bash
   dotnet restore
   ```

2. **Migrációk alkalmazása:**
   ```bash
   cd src/services/Template.Api
   dotnet ef database update
   ```

3. **Core API indítása:**
   ```bash
   dotnet run --project src/services/Template.Api
   ```

4. **Products API indítása (Proxy módnál):**
   ```bash
   dotnet run --project src/services/Template.Products.Api
   ```

5. **Gateway indítása (Proxy módnál):**
   ```bash
   dotnet run --project src/services/Template.Gateway.Api
   ```

### Tesztelés

```bash
dotnet test tests/Template.Tests/Template.Tests.csproj
```

**Aktuális állapot:** 146/146 sikeres (unit + integration)

### OpenAPI kliens generálás

OpenAPI specifikációk a frontend által használva:
- `Api.json` - Core API spec
- `ProductsApi.json` - Products API spec

Újragenerálás:
```powershell
.\scripts\Refresh-OpenApi.ps1
```

## Környezeti változók

| Valtozo | Leírás | Alapérték |
|---------|--------|-----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | - |
| `JwtSettings__Secret` | JWT aláíró titok (min. 32 karakter) | - |
| `JwtSettings__Issuer` | JWT kibocsátó | `template-api` |
| `JwtSettings__Audience` | JWT célközönség | `template-frontend` |
| `InternalServiceAuth__Token` | Szolgáltatás-közötti auth token (min. 32 karakter) | - |
| `ProductsService__GrpcBaseUrl` | Products gRPC endpoint (Proxy mod) | - |
| `ASPNETCORE_ENVIRONMENT` | Környezet: Development, Staging, Production | `Development` |
| `SystemAdmin__Email` | Első indításkor seedelt system admin email | `admin@template.io` |

Lásd [.env.example](../.env.example) a Docker Compose konfigurációkhoz.

## Docker secrets

A Docker secrets felülírja a környezeti változókat ha mindkettő létezik.

**Támogatott secretek:**
- `jwt_secret` -> `JwtSettings:Secret`
- `internal_service_token` -> `InternalServiceAuth:Token`
- `db_password` -> `Database:Password`

**Használat:**
```yaml
secrets:
  jwt_secret:
    file: ./secrets/jwt_secret.txt
  internal_service_token:
    file: ./secrets/internal_service_token.txt
```

A `AddDockerSecrets()` extension automatikusan betölti a `/run/secrets/` útvonalról.

## Biztonsági funkciók

### Hitelesítés es jogosultságkezelés
- SHA-256 hash-elt refresh tokenek (nincs plaintext tárolás)
- DataProtection kulcsok perzisztálva `/app/keys` volume-ra
- Rate limiting: 10 kérés/perc auth endpointokon (Development-ben kikapcsolva)
- Szerepkör alapú jogosultság policy-k

### Szolgáltatás-közötti biztonság
- Belső szolgáltatás hitelesítés token validációval
- Konstans idejű token összevetés (`CryptographicOperations.FixedTimeEquals`)
- Minimum 32 karakteres token hossz követelmény

### Adatvédelem
- BOLA védelem `QueryGuard<T>`-vel (létesítményen kívüli hozzáférés megakadályozása)
- Query string redaction (passwords, tokens, refresh tokens, access tokens)
- Kérés telemetria CPU/RAM monitorozással
- Resource guard middleware (kooperatív megszakítás terhelés alatt)

### Audit
- Entitás változás követés `AuditInterceptor`-ral (singleton, Facility + Product entitásokra)
- Audit log szűréssel: entitás tipus, akció (Created/Updated/Deleted), felhasználó, dátumtartomány
- N+1 query megelőzés (batch lekérdezések, `ExecuteUpdateAsync`)

## Optimistic concurrency

Az optimistic concurrency a PostgreSQL `xmin` rendszeroszlopára épül.

**Működés:**
1. Minden entitás (Entity base class) tartalmaz `RowVersion` propertyt (`uint`)
2. EF Core konfig: `.HasColumnName("xmin").HasColumnType("xid").IsRowVersion()`
3. Update kérésnél a kliens elküldi a korábbról lekérdezett `RowVersion`-t
4. Ha az adatbázisban más érték van (más változtatta közben), `DbUpdateConcurrencyException` dobódik
5. Handler 409 Conflict-et ad vissza `ProductErrors.ConcurrencyConflict` vagy hasonló hibaként

**Érintett entitások:** Facility, Product, MailboxMessage

## Idempotencia (IdempotencyMiddleware)

POST kérések biztonságos újrapróbálhatók az `Idempotency-Key` headerrel.

**Működés:**
1. Kliens küld `Idempotency-Key` headert (egyedi azonosító)
2. Middleware ellenőrzi az in-memory cache-t
3. Ha már létező kulcs: visszaadja a cached választ `X-Idempotency-Replayed: true` headerrel
4. Ha új kulcs: végrehajtja a kérést, cache-eli a választ 24 óráig

**Kizárt útvonalak:** `/health`, `/scalar`, `/openapi`

**Megjegyzés:** In-memory implementáció, egyetlen node-ra alkalmas. Többpéldányos deploymenthez Redis vagy más elosztott cache ajánlott.

## Circuit breaker (ResourceGuardMiddleware)

Automatikus terhelés védelem CPU és RAM küszöbértékek alapján.

**Állapotok:**
- **Closed:** Normál működés, kérések feldolgozva
- **Open:** Küszöb átlépve, összes kérés 503-at kap `Retry-After` headerrel
- **HalfOpen:** Próba kérés átengedve, siker esetén Closed, különben vissza Open

**Konfiguráció (ResourceGuardOptions):**
| Beallitas | Alapérték | Leírás |
|-----------|-----------|--------|
| `CpuThresholdPercent` | 90 | CPU küszöb az Open állapothoz |
| `MemoryThresholdPercent` | 95 | RAM küszöb az Open állapothoz |
| `CircuitBreakerDurationSeconds` | 30 | Open állapot időtartama |
| `MaxWorkingSetMb` | 1024 | Max munkakészlet MB-ban |
| `MaxConcurrentRequests` | 200 | Max párhuzamos kérésszám |
| `MinAvailableWorkerThreads` | 16 | Min szabad worker thread-ek |

## Middleware pipeline

```
RequestTelemetryMiddleware
↓
ResourceGuardMiddleware
↓
RateLimiter (nem-Development)
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

## Háttérfeladatok (TickerQ)

**Dashboard:** `/admin/tickerq` (SystemAdmin szerepkör szükséges)

**Példák:**
- `DemoLongRunningOperation` - Manuális trigger demo
- `Demo.Heartbeat` - Cron-alapú ütemezett feladat

**Konfiguráció:**
```csharp
builder.Services.AddBackgroundJobs<AppDbContext>(
    dashboardBasePath: "/admin/tickerq",
    dashboardPolicyName: "TickerQDashboard"
);
```

## Adatbázis

**Migrációk:**
1. Init
2. AddRefreshToken
3. AddPreferredLanguage
4. AddTickerQBackgroundJobs
5. AddMailboxMessages
6. AddProductQuantity
7. AddAuditLog
8. HashRefreshToken (plaintext tokenek SHA-256 hash-re migráció)
9. AddOptimisticConcurrency (PostgreSQL xmin oszlop EF Core mapping)

**Auto-migráció:** Development módban induláskor aktív

**Seeding:**
- Szerepkörök: SystemAdmin, FacilityAdmin, FacilityEditor, FacilityViewer
- Alapértelmezett admin: `admin@template.io` (konfigurálható: `SystemAdmin:Email`) - jelszó-visszaállítási email küldve az első indításkor

## Cache

**GetProducts:**
- In-memory cache létesítményenként (`products:facility:{id}`)
- 5 perces TTL
- Invalidáció: Create/Update product
- Rendezés/keresés/lapozás memóriában cache lekérés után

**GetAllUsers:**
- In-memory cache (5 perces TTL)
- Invalidacio: Create/Approve/Delete/UpdateRole

## Health checkek

| Endpoint | Cél |
|----------|-----|
| `/health/live` | Liveness probe (mindig 200-at ad vissza) |
| `/health/ready` | Readiness probe (adatbázis kapcsolat ellenőrzése) |
| `/health` | Kombinált egészségi állapot |

## Rugalmasság (Polly)

**SMTP Email szolgáltatás:**
- 3 újrapróbálkozás 2 másodperces késleltetéssel
- Kihagyja az újrapróbálkozást hitelesítési hibáknál

**gRPC Facility Usage szolgáltatás:**
- 3 újrapróbálkozás exponenciális backoff-fal (1s alap)
- Kihagyja az újrapróbálkozást invalid argument hibáknál

## Docker

**Image-ek:**
- `Dockerfile` - Core API
- `Dockerfile.ProductsApi` - Products API
- `Dockerfile.GatewayApi` - Gateway API

**Build a repository gyökeréből:**
```bash
docker compose up -d --build
```

**Volume-ok:**
- `dataprotection-keys` - Megosztott DataProtection kulcsok konténetek között
- `postgres-data` - PostgreSQL adat perzisztencia (named volume; `docker compose down -v` reseteli)

**Secrets:**
- `secrets/jwt_secret.txt`
- `secrets/internal_service_token.txt`

## API dokumentáció

**Scalar UI:** http://localhost:8080/scalar/v1

Bearer token hitelesítési séma a hitelesített endpointok teszteléséhez.

## Megjegyzések

- **Mediator:** `Mediator` NuGet csomagot használ (`using Mediator;`), nem MediatR-t
- **ErrorOr:** Funkcionális hibakezelési minta (nincs kivétel üzleti logika hibákhoz)
- **Entity base class:** `Domain/Common/Entity.cs` `Guid Id`, `DateTime CreatedAt` és `uint RowVersion`-nel
- **QuestPDF:** Community licensz (ingyenes open-source/kiértékeléshez, kereskedelmi felhasználáshoz vásárlás szükséges)
