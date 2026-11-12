# .NET Template Projekt

[English](README.md) | **Magyar**

Egy full-stack referencia-sablon, ami production-orientált .NET és Angular fejlesztési mintákat mutat be valós funkcionalitással. A mellékelt Docker Compose konfiguráció lokális fejlesztésre készült (`ASPNETCORE_ENVIRONMENT=Development`), nem éles telepítésre.

Ez egy **demonstrációs sablon**, nem üzleti alkalmazás. Minden feature enterprise-szintű implementációs mintákat mutat be: hitelesítés, jogosultságkezelés, háttérfeladatok, PDF/Excel generálás, microservice-ek, gRPC és még sok más.

## Architektúra

```
                 ┌──────────────┐
  böngésző ─:80─>│ frontend     │ Angular, nginx szolgálja ki (CSP, X-Frame-Options)
                 └──────┬───────┘
                        │ /api
                 ┌──────v───────┐
         :8080 ─>│ api (gateway)│ YARP reverse proxy, CORS
                 └──┬────────┬──┘
     /api/products/*│        │ minden más
          ┌─────────v──┐  ┌──v─────────┐
          │products-api│<─┤ core-api   │ auth, admin, felhasználók, létesítmények, jobok
          │ REST + gRPC│  │            │ gRPC kliens (belső szolgáltatás-token)
          └─────┬──────┘  └──┬─────────┘
                └─────┬──────┘
               ┌──────v──────┐   ┌─────────────┐
               │ PostgreSQL  │<──┤ db-migrator │ egyszer lefutó EF Core migrációs job
               └─────────────┘   └─────────────┘
```

Backend felépítés (`backend/src`):

| Mappa | Projektek |
|---|---|
| `services/` | `Template.Api` (core API), `Template.Products.Api` (termékek, gRPC szerver), `Template.Gateway.Api` (YARP), `Template.Migrator` |
| `core/` | `Template.Application` (CQRS commandok/query-k, FluentValidation), `Template.Domain`, `Template.Infrastructure` (EF Core, Identity, auth, query guardok), `Template.Grpc` |
| `common/` | middleware-ek, naplózás, konfigurációs extensionök, Email, Excel, Jobs, Pdf, Templating |

Részletek: [backend/README.hu.md](backend/README.hu.md), [frontend/README.hu.md](frontend/README.hu.md).

## Tech Stack

| Réteg | Technológia |
|-------|-------------|
| **Backend** | .NET 10, ASP.NET Core Web API |
| **Frontend** | Angular 22, Standalone Components, Signals |
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
   git clone https://github.com/barcsaidominik/dotnet-template.git
   cd dotnet-template
   cp .env.example .env
   ```

2. **Docker secrets létrehozása (véletlen értékekkel, mindkettő min. 32 karakter):**
   ```bash
   openssl rand -base64 48 > secrets/jwt_secret.txt
   openssl rand -base64 48 > secrets/internal_service_token.txt
   ```
   PowerShell alatt:
   ```powershell
   [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48)) | Set-Content -NoNewline secrets/jwt_secret.txt
   [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48)) | Set-Content -NoNewline secrets/internal_service_token.txt
   ```
   A két fájlnak a `docker compose up` előtt léteznie kell. Ha egy korábbi futás könyvtárat
   hozott létre ezeken a neveken (`secrets/jwt_secret.txt/`), töröld a könyvtárat, és hozd létre fájlként.

3. **Összes szolgáltatás indítása:**
   ```bash
   docker compose up -d --build
   ```

   A `db-migrator` egy önálló, egyszer lefutó konténer: alkalmazza az EF Core migrációkat,
   majd sikeres futás (exit 0) után leáll. A `core-api` és a `products-api` a
   `service_completed_successfully` feltétellel várja meg ezt a konténert, így egyik API sem
   fut neki egy még nem migrált sémának. A séma tulajdonlása explicit: egyik API sem migrál
   induláskor. A `Template.Api` csak seedel (szerepkörök, system admin), és migrálatlan sémán
   leáll. Compose nélkül a migrátort kézzel kell futtatni, lásd [backend/README.hu.md](backend/README.hu.md).

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
- Használt secretek: `jwt_secret`, `internal_service_token`
- Secretek felülírják a környezeti változókat ha mindkettő létezik
- Az adatbázis-kapcsolat a `ConnectionStrings:DefaultConnection` értékből jön; ha üres, a szolgáltatások nem indulnak el

### Microservice-ek és kommunikáció
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
- 154 backend teszt (xUnit v3, unit + `WebApplicationFactory`/EF InMemory integrációs tesztek)
- 88 frontend Vitest teszt (21 teszt fájl)

## Projekt struktura

```
dotnet-template/
├── backend/              <- .NET 10 backend szolgáltatások
├── frontend/             <- Angular 22 frontend
├── docker-compose.yml    <- Multi-container orchestration
├── .env.example          <- Környezeti változó sablon
└── secrets/              <- Docker secrets (kézi létrehozás)
```

## Dokumentacio

- [Backend README](backend/README.hu.md) - Architektúra, deployment módok, API részletek
- [Frontend README](frontend/README.hu.md) - Angular setup, i18n, tesztelés

## Alapértelmezett bejelentkezési adatok

Az első indítás után egy system admin kerül seedelésre az `admin@template.io` email címre (konfigurálható: `SystemAdmin:Email` appsettings értékkel).

Az adminfióknak nincs alapértelmezett jelszava - az alkalmazás az első indításkor jelszó-visszaállítási emailt küld a megadott email címre. A linkre kattintva állítható be a jelszó a `/auth/set-password` oldalon.

**Production-ban:** Állítsd be a `SystemAdmin__Email` környezeti változót (vagy `SystemAdmin:Email` appsettings értéket) a saját email címedre, hogy megkapd a beállítási linket az első indításkor.

## Licenc

[MIT](LICENSE). A harmadik féltől származó függőségekre a saját licencük vonatkozik (lásd lent: QuestPDF).

## Megjegyzesek

- **QuestPDF:** Community licensz (ingyenes open-source és kiértékeléshez). Kereskedelmi felhasználáshoz vásárlás szükséges.
- **Data Protection Keys:** `/app/keys` volume-ra perzisztálva a token konzisztencia érdekében újraindítás után is
- **Adatbázis migrációk:** kizárólag a `Template.Migrator` (Compose alatt a `db-migrator` konténer) alkalmazza
- **Diplomamunka-hibaminták:** A lezárt, nem merge-ölt PR-ek (pl. #41) szándékosan sebezhető kódot és kitalált titkokat tartalmaznak egy mérési kísérlethez; ezek nem részei a `master` ágnak
