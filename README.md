# .NET Template Projekt

Egy production-ready full-stack sablon, ami modern .NET és Angular fejlesztési mintákat mutat be valós funkcionalitással.

Ez egy **demonstrációs sablon**, nem üzleti alkalmazás. Minden feature enterprise-szintű implementációs mintákat mutat be: hitelesítés, jogosultságkezelés, háttérfeladatok, PDF/Excel generálás, microservice-ek, gRPC és még sok más.

## Tech Stack

| Réteg | Technológia |
|-------|-------------|
| **Backend** | .NET 10, ASP.NET Core Web API |
| **Frontend** | Angular 20, Standalone Components, Signals |
| **Architektúra** | Clean Architecture, CQRS (Mediator) |
| **Adatbázis** | PostgreSQL 17, EF Core |
| **Hitelesítés** | ASP.NET Core Identity, JWT + Refresh Tokens |
| **Háttérfeladatok** | TickerQ (EF Core store) |
| **Dokumentum generálás** | QuestPDF (PDF), ClosedXML (Excel) |
| **Kommunikáció** | gRPC, YARP Reverse Proxy |
| **Rugalmasság** | Polly (retry policy-k) |

## Gyors indítás

1. **Repo klónozása és környezet konfigurálása:**
   ```bash
   git clone <repository-url>
   cd dotnet-template
   cp .env.example .env
   ```

2. **Docker secrets létrehozása:**
   ```bash
   mkdir -p secrets
   echo "your-jwt-secret-at-least-32-characters-long" > secrets/jwt_secret.txt
   echo "your-internal-service-token-here" > secrets/internal_service_token.txt
   ```

3. **Összes szolgáltatás indítása:**
   ```bash
   docker compose up -d --build
   ```

4. **Alkalmazás elérése:**
   - Frontend: http://localhost
   - API dokumentáció (Scalar): http://localhost:8080/scalar/v1
   - TickerQ Dashboard: http://localhost:8080/admin/tickerq (SystemAdmin szerepkör szükséges)

## Funkciók

### Hitelesítés és jogosultságkezelés
- **Regisztrációs folyamat:** Regisztráció -> Admin jóváhagyás -> Szerepkör hozzárendelés
- **JWT hitelesítés:** 15 perces access tokenek, 7 napos refresh tokenek
- **Szerepkör alapú hozzáférés:** SystemAdmin, FacilityAdmin, FacilityEditor, FacilityViewer
- **Biztonságos token tárolás:** SHA-256 hash-elt refresh tokenek, DataProtection kulcs perzisztencia

### Felhasználókezelés
- Admin felhasználó jóváhagy workflow
- Létesítmény alapú felhasználó szervezés
- Szerepkör kezelés és módosítás
- Email módosítás (PUT /api/Admin/users/{userId})
- Excel export lokalizációval

### Létesítmények és termékek
- Multi-tenant létesítmény struktúra
- Termék CRUD lapozással, rendezéssel és kereséssel
- Excel import/export (lokalizált)
- PDF rendelés generálás (QuestPDF + Scriban sablonok)
- In-memory cache intelligens invalidációval

### Optimistic concurrency
- PostgreSQL `xmin` rendszeroszlopra épül
- `Entity.RowVersion` property minden entitáson (Facility, Product, MailboxMessage)
- 409 Conflict válasz párhuzamossági ütközés esetén
- Frontendről küldött `RowVersion` alapú verzióellenőrzés

### Idempotencia
- `Idempotency-Key` header POST kéréseknél
- 24 órás in-memory cache a válaszokhoz
- `X-Idempotency-Replayed: true` header újrajátszott válaszoknál
- Health/OpenAPI/Scalar útvonalak kizárva

### Circuit breaker (ResourceGuardMiddleware)
- CPU és RAM küszöbértékek (90% / 95% alapértelmezett)
- Állapotok: Closed -> Open -> HalfOpen
- 503 Service Unavailable válasz túlterhelés esetén
- `Retry-After` header a várakozási idővel

### Docker secrets
- `AddDockerSecrets("/run/secrets/")` konfigurációs extension
- Támogatott secretek: `jwt_secret`, `internal_service_token`, `db_password`
- Secretek felülírják a környezeti változókat ha mindkettő létezik

### Microservice-ek és kommunikáció
- **InProcess mod:** Monolitikus deployment
- **Proxy mod:** Gateway API YARP routing-gal külön szolgáltatásokhoz
- **gRPC:** Szolgáltatások közti kommunikáció Polly retry policy-kkal
- **Belső szolgáltatás hitelesítés:** Token alapú service-to-service auth

### Háttérfeladatok
- TickerQ integráció EF Core tárolással
- Dashboard UI feladat monitorozáshoz
- Cron-alapú ütemezés demo
- Hosszan futó művelet példa

### Audit és monitorozás
- Entitás változás követés (Facilities, Products)
- Audit log szűréssel (entitás típus, akció, dátumtartomány, felhasználó)
- Serilog Syslog RFC 5424 formátummal
- Health checkek (liveness, readiness)
- Kérés telemetria (CPU, RAM, query redaction)

### Postafiók
- Alkalmazáson belüli üzenetküldés
- Olvasatlan darabszám követés
- Valós idejű frissítések

### Biztonság
- Rate limiting (10 kérés/perc auth endpointokon, csak nem-Development)
- BOLA védelem QueryGuard-dal
- Konstans idejű token összevetés (gRPC)
- Query string redaction (passwords, tokens, refresh tokens, access tokens)
- CSP, X-Frame-Options, Referrer-Policy headerek

### Fejlesztői élmény
- Clean Architecture világos szeparációval
- ErrorOr eredmény minta
- FluentValidation pipeline behavior-ral
- OpenAPI/Scalar dokumentáció
- Hot reload támogatás
- 133 backend teszt (unit + integration)
- 88 frontend Vitest teszt

## Projekt struktura

```
dotnet-template/
├── backend/              <- .NET 10 backend szolgáltatások
├── frontend/             <- Angular 20 frontend
├── docker-compose.yml    <- Multi-container orchestration
├── .env.example          <- Környezeti változó sablon
└── secrets/              <- Docker secrets (kézi létrehozás)
```

## Dokumentacio

- [Backend README](backend/README.md) - Architektúra, deployment módok, API részletek
- [Frontend README](frontend/README.md) - Angular setup, i18n, tesztelés

## Alapértelmezett bejelentkezési adatok

Az első indítás után egy system admin kerül seedelésre:
- **Email:** `admin@example.com`
- **Jelszo:** `Admin123!`

**Production-ben azonnal változtasd meg.**

## Licenc

Ez a sablon demonstrációs célokat szolgál. A licenszet a felhasználási célodnak megfelelően változtathatod.

## Megjegyzesek

- **QuestPDF:** Community licensz (ingyenes open-source és kiértékeléshez). Kereskedelmi felhasználáshoz vásárlás szükséges.
- **Data Protection Keys:** `/app/keys` volume-ra perzisztálva a token konzisztencia érdekében újraindítás után is
- **Adatbázis migrációk:** Automatikusan alkalmazva induláskor Development módban
