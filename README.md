# Template App

Bemutatható full-stack sablonprojekt modern .NET és Angular stackkel, több szolgáltatásra bontható backenddel, generált API klienssel, háttérjobokkal, dokumentum-exporttal és többnyelvű felülettel.

## Mi ez a projekt?

Ez a repository egy olyan induló alap, ami egyszerre alkalmas:
- új üzleti alkalmazások gyors indítására,
- referenciaprojektként történő bemutatásra,
- és fokozatos monolit -> microservice átmenet demonstrálására.

A megoldás jelenleg:
- `ASP.NET Core` + `Angular` alapú,
- `Clean Architecture` szemléletet követ,
- `CQRS` és `Mediator` mintát használ,
- `PostgreSQL`-re épül,
- támogat `JWT` + refresh token autentikációt,
- tartalmaz `gRPC` service-to-service kommunikációt,
- és Docker Compose alatt több szolgáltatásként is futtatható.

## Fő képességek

### Backend
- felhasználókezelés több szerepkörrel
- regisztráció, jóváhagyás, jelszóbeállítás, bejelentkezés, refresh token
- üzemek és termékek kezelése
- Excel export / import
- PDF generálás
- postaláda / értesítési felület
- háttérjobok `TickerQ`-val
- request telemetry és resource guard middleware
- OpenAPI alapú kliensgenerálás
- belső `gRPC` hívás a service boundary-k között

### Frontend
- Angular 20 alapú admin/facility/public felületek
- generált TypeScript API kliens
- magyar és angol lokalizáció
- sötét mód
- admin, facility és public termékoldalak
- postaláda badge + részletes postaláda oldal
- szigorú ESLint + Prettier + git hook alapú code style
- Vitest alapú frontend unit tesztek

### Minőség és üzemeltetés
- backend unit + integration tesztek
- frontend unit tesztek
- Docker Compose alapú többkonténeres futtatás
- külön secret- és configkezelési minta
- strukturált logolás és érzékeny query paraméterek redakciója

## Architektúra röviden

### Backend szolgáltatások
- `Template.Gateway.Api`: reverse proxy bejárat
- `Template.Api`: core API, auth, admin, facility users, mailbox, orchestráció
- `Template.Products.Api`: külön products service, REST + gRPC végpontokkal

### Core rétegek
- `Template.Domain`: entitások, invariánsok, hibák
- `Template.Application`: use case-ek, CQRS handlerek, validációs pipeline
- `Template.Infrastructure`: EF Core, identity, külső integrációk, szolgáltatásimplementációk
- `Template.Grpc`: közös proto/stub projekt

### Common csomagok
- email
- excel
- jobs
- pdf
- templating
- shared common elemek

## Gyors indítás

### 1. Docker Compose

1. Másold a [.env.example](.env.example) fájlt `.env` néven.
2. Töltsd ki legalább a JWT és a belső service token értékeket.
3. Indítsd el:

```powershell
docker compose up -d --build
```

Compose alatt a fő elemek:
- `db`
- `gateway api`
- `core api`
- `products api`
- `frontend`

### 2. Lokális fejlesztés

Backend:

```powershell
dotnet restore backend/Template.slnx
dotnet build backend/Template.slnx
dotnet test backend/tests/Template.Tests/Template.Tests.csproj
```

Frontend:

```powershell
cd frontend
npm install
npm run generate:api
npm start
```

Ha a backend OpenAPI leírása változik, a frontend kliens frissítése:

```powershell
cd frontend
npm run generate:api
```

## Repository felépítése

```text
dotnet-template/
|- backend/
|  |- src/common/
|  |- src/core/
|  |- src/services/
|  |- tests/
|  |- SECRETS_SETUP.md
|- frontend/
|  |- src/
|  |- OPENAPI_PLAYBOOK.md
|- docker-compose.yml
|- .env.example
|- todo.md
```

## Fontos workflow-k

### Auth és onboarding
- regisztráció
- admin jóváhagyás
- setup link / jelszóbeállítás
- JWT + refresh token

### Admin műveletek
- felhasználók listázása és jóváhagyása
- üzemek kezelése
- facility user létrehozás
- exportok

### Products
- külön products service
- import/export/PDF
- facility és public nézetek
- gRPC alapú facility-ellenőrzés törlés előtt

### Mailbox és háttérfolyamatok
- postaláda események
- háttérjobok `TickerQ`-val
- adminból ütemezhető demo jobok

## Quality bar

Jelenlegi fontosabb ellenőrzések:
- backend tesztek: `114/114` zöld
- frontend tesztek: `88/88` zöld
- frontend production build: sikeres

Frontend style gate:
- `npm run style:check`
- pre-commit hook `lint-staged`-del

## További dokumentáció

- backend részletes leírás: [backend/README.md](backend/README.md)
- frontend részletes leírás: [frontend/README.md](frontend/README.md)
- secret setup: [backend/SECRETS_SETUP.md](backend/SECRETS_SETUP.md)
- OpenAPI workflow: [frontend/OPENAPI_PLAYBOOK.md](frontend/OPENAPI_PLAYBOOK.md)
- aktuális backlog/lezárások: [todo.md](todo.md)

## Mikor jó választás ez a template?

Ez a projekt akkor különösen erős kiindulópont, ha:
- gyorsan szeretnél indulni enterprise jellegű .NET + Angular alappal,
- fontos a tiszta rétegzés és a későbbi szétválaszthatóság,
- kell adminfelület, auth, export, dokumentumkezelés és auditálható működés,
- és nem nulláról akarod minden alkalommal újra felépíteni a platformrészeket.
