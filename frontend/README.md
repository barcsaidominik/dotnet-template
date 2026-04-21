# Frontend

Angular 20 frontend standalone komponensekkel, signalokkal, Material Design-nal és generált OpenAPI kliensekkel.

## Tech Stack

| Komponens | Technológia |
|-----------|-------------|
| **Framework** | Angular 20 |
| **UI könyvtár** | Angular Material 20 |
| **Architektúra** | Standalone komponensek, Signals |
| **Állapotkezelés** | RxJS, Angular Signals |
| **Routing** | Angular Router guardokkal |
| **i18n** | ngx-translate (magyar, angol) |
| **API kliens** | OpenAPI-ból generálva (ng-openapi-gen) |
| **Tesztelés** | Vitest 3 |
| **Linting** | ESLint 9 + typescript-eslint |
| **Formátás** | Prettier 3 |
| **Git hookok** | simple-git-hooks + lint-staged |

## Funkciók

### Hitelesítés és felhasználókezelés
- Bejelentkezés, regisztráció, jelszóbeállítás
- JWT + refresh token kezelés
- Auth guard és role guard
- Automatikus token frissítés 401 esetén
- Admin felhasználó jóváhagy workflow

### Admin oldalak (SystemAdmin szerepkör)
- **Felhasználók:** Lista, jóváhagy, szerepkör hozzárendelés, törlés, Excel export, email módosítás
- **Létesítmények:** CRUD, felhasználó hozzárendelés, Excel export
- **Audit log:** Szűrés entitás típus, akció, dátumtartomány, felhasználó alapján

### Létesítmény oldalak (FacilityAdmin szerepkör)
- **Felhasználók:** Létesítmény felhasználók létrehozása, szerepkör hozzárendelés, eltávolítás, keresés, rendezés
- **Termékek:** CRUD lapozással, rendezéssel, kereséssel, Excel import/export, PDF generálás

### Termékek (FacilityEditor/FacilityViewer szerepkörök)
- Csak olvasási tereklista lapozással és rendezéssel

### Postafiók
- Alkalmazáson belüli üzenetküldés
- Olvasatlan darabszám badge
- Megjelölés olvasott/olvasatlan
- Valós idejű frissítések

### i18n
- Magyar és angol fordítások
- Dátum lokalizáció szinkronizálás
- Backend preferencia szinkron
- Nyelvváltó a shellben

### Téma
- Világos és sötét mód
- Perzisztens felhasználói preferencia
- Material Design 3

## Fejlesztés

### Előfeltételek
- Node.js 20+
- npm 10+

### Függőségek telepítése

```bash
npm install
```

### Fejlesztői szerver

```bash
npm start
```

Az alkalmazás a http://localhost:4200 címen fut, proxy-val a backend gateway-hez (http://localhost:8080).

**Proxy konfiguráció:** [proxy.conf.json](proxy.conf.json)

### Build

```bash
npm run build
```

Production build a `dist/` könyvtárba kerül.

### Tesztelés

```bash
npm run test
```

**Aktuális állapot:** 88/88 sikeres (Vitest unit tesztek)

**Egyszeri futtatás:**
```bash
npm run test:run
```

### Kódminőség

**Lint:**
```bash
npm run lint
npm run lint:fix
```

**Formátás:**
```bash
npm run format
npm run format:check
```

**Teljes stílusellenőrzés:**
```bash
npm run style:check
```

## OpenAPI kliens generálás

A frontend generált TypeScript klienseket használ a backend OpenAPI specifikációkból.

### Összes kliens generálása

```bash
npm run generate:api
```

Ez futtatja:
1. `generate:types` - TypeScript típusok generálása az `Api.json`-ból
2. `generate:client` - Angular szolgáltatások generálása az `Api.json`-ból
3. `generate:products` - Típusok és szolgáltatások generálása a `ProductsApi.json`-ból

### Manuális lépések

**Core API:**
```bash
npm run generate:types
npm run generate:client
```

**Products API:**
```bash
npm run generate:products
```

**Konfigurációs fájlok:**
- [ng-openapi-gen.json](ng-openapi-gen.json) - Core API konfig
- [ng-openapi-gen-products.json](ng-openapi-gen-products.json) - Products API konfig

**Generált kód helye:**
- `src/app/generated/client/` - Core API szolgáltatások
- `src/app/generated/products-client/` - Products API szolgáltatások
- `src/app/generated/api.types.ts` - Core API típusok
- `src/app/generated/products.api.types.ts` - Products API típusok

**Automatizált generálás:**
```powershell
.\scripts\Refresh-OpenApi.ps1
```
(Elindítja a backend szolgáltatásokat, letölti a specifikációkat, generálja a klienseket)

## Projekt struktúra

```
src/app/
├── core/                   <- Core szolgáltatások és guardok
│   ├── auth/               <- Auth szolgáltatás, guardok, interceptorok
│   ├── i18n/               <- Nyelv szolgáltatás
│   ├── mailbox/            <- Mailbox szolgáltatás
│   ├── models/             <- Közös modellek
│   ├── services/           <- Egyéb core szolgáltatások
│   └── theme/              <- Téma szolgáltatás
├── features/               <- Feature modulok
│   ├── admin/              <- Admin oldalak (users, facilities, audit-log)
│   ├── auth/               <- Auth oldalak (login, register, set-password)
│   ├── facility/           <- Létesítmény oldalak (users, products)
│   ├── mailbox/            <- Postafiók oldal
│   └── products/           <- Termékek oldal (FacilityEditor/Viewer)
├── generated/              <- Generált OpenAPI kliensek
│   ├── client/             <- Core API kliens
│   ├── products-client/    <- Products API kliens
│   ├── api.types.ts        <- Core API típusok
│   └── products.api.types.ts  <- Products API típusok
├── shared/                 <- Megosztott komponensek és segédfüggvények
│   ├── components/         <- Újrafelhasználható komponensek
│   ├── shell/              <- Shell layout navigációval
│   └── utils/              <- Segédfüggvények, mapperek
└── app.routes.ts           <- Alkalmazás routing
```

## Útvonalak

| Utvonal | Komponens | Guard | Leírás |
|---------|-----------|-------|--------|
| `/auth/login` | AuthLoginComponent | - | Bejelentkező oldal |
| `/auth/register` | AuthRegisterComponent | - | Regisztrációs oldal |
| `/auth/set-password` | AuthSetPasswordComponent | - | Jelszóbeállítás (létesítmény felhasználók) |
| `/admin/users` | AdminUsersPageComponent | authGuard, roleGuard(SystemAdmin) | Felhasználókezelés |
| `/admin/facilities` | AdminFacilitiesPageComponent | authGuard, roleGuard(SystemAdmin) | Létesítménykezelés |
| `/admin/audit-log` | AdminAuditLogPageComponent | authGuard, roleGuard(SystemAdmin) | Audit log megjelenítő |
| `/facility/users` | FacilityUsersPageComponent | authGuard, roleGuard(FacilityAdmin) | Létesítmény felhasználókezelés |
| `/facility/products` | FacilityProductsPageComponent | authGuard, roleGuard(FacilityAdmin) | Létesítmény termékkezelés |
| `/products` | ProductsPageComponent | authGuard, roleGuard(FacilityEditor, FacilityViewer) | Tereklista (csak olvasás) |
| `/mailbox` | MailboxPageComponent | authGuard | Postafiók üzenetek |

**Útvonal definíciók:** [app.routes.ts](src/app/app.routes.ts)

## i18n (Nemzetköziesítés)

**Támogatott nyelvek:**
- Magyar (hu)
- Angol (en)

**Fordítás fájlok:**
- [src/assets/i18n/hu.json](src/assets/i18n/hu.json)
- [src/assets/i18n/en.json](src/assets/i18n/en.json)

**Használat sablonokban:**
```html
<h1>{{ 'auth.login.title' | translate }}</h1>
```

**Használat kódban:**
```typescript
this.translateService.instant('auth.login.success');
```

**Nyelvváltás:**
- Felhasználó válthat a shell legülővel
- Preferencia mentve backendre
- Dátum lokalizáció automatikusan szinkronizálva

## Téma rendszer

**ThemeService:**
- Világos/sötét mód váltás
- Perzisztens preferencia (localStorage)
- Material Design 3 témázás

**Használat:**
```typescript
this.themeService.toggleTheme();
this.themeService.isDark(); // Signal<boolean>
```

## Kódstílus és Git hookok

**Konfiguráció:**
- [.editorconfig](.editorconfig) - Editor beállítások
- [eslint.config.js](eslint.config.js) - ESLint szabályok
- [.prettierrc.json](.prettierrc.json) - Prettier formátás

**Pre-commit hook:**
- `lint-staged` fut a staged fájlokon
- ESLint + Prettier `.ts` és `.html` fájlokon
- Csak Prettier `.scss`, `.css`, `.json`, `.js`, `.md` fájlokon

**Beállítás:**
```bash
npm run prepare
```

## Tesztelési stratégia

**Vitest konfiguráció:** [vitest.config.ts](vitest.config.ts)

**Teszt lefedettség:**
- Auth szolgáltatás és guardok
- Nyelv szolgáltatás
- Téma szolgáltatás
- Postafiók komponensek és szolgáltatás
- Admin oldalak (users, facilities)
- Létesítmény oldalak (users, products)
- Termékek oldal
- Shell komponens
- Mapperek és segédfüggvények

**Tesztek futtatása:**
```bash
npm run test          # Watch mod
npm run test:run      # Egyszeri futtatás
```

## Docker

**Dockerfile:** [Dockerfile](Dockerfile)

**Build:**
```bash
docker build -t template-frontend .
```

**Futtatás:**
```bash
docker run -p 80:80 template-frontend
```

**Docker Compose:**
```bash
docker compose up -d --build
```

A frontend nginx-en keresztül szolgáltatva reverse proxy-val a backend gateway-hez.

**nginx konfiguráció:** [nginx.conf](nginx.conf)

## Biztonsági headerek (nginx)

- `Content-Security-Policy` - Erőforrás betöltés korlátozás
- `X-Frame-Options: DENY` - Clickjacking megelőzés
- `X-Content-Type-Options: nosniff` - MIME sniffing megelőzés
- `Referrer-Policy: strict-origin-when-cross-origin` - Referrer kontrollás
- `Permissions-Policy` - Felesleges böngésző funkciók kikapcsolása

## Környezet konfiguráció

A frontend az Angular környezeti rendszerét használja.

**Development:**
- API proxy a `http://localhost:8080` címre
- Source mapek aktív
- Development mod

**Production:**
- API base URL: `/api` (nginx szolgáltatja)
- Optimalizált build
- AOT fordítás

## NPM scriptek

| Script | Cél |
|--------|-----|
| `npm start` | Fejlesztői szerver (http://localhost:4200) |
| `npm run build` | Production build |
| `npm run watch` | Watch mod build |
| `npm run test` | Vitest watch mod |
| `npm run test:run` | Vitest egyszeri futtatás |
| `npm run lint` | ESLint ellenőrzés |
| `npm run lint:fix` | ESLint auto-fix |
| `npm run format` | Prettier formátás |
| `npm run format:check` | Prettier ellenőrzés |
| `npm run style:check` | Teljes stíluskapu (format + lint) |
| `npm run generate:api` | Összes OpenAPI kliens generálása |
| `npm run generate:types` | Core API típusok generálása |
| `npm run generate:client` | Core API szolgáltatások generálása |
| `npm run generate:products` | Products API kliens generálása |
| `npm run prepare` | Git hookok beállítása |

## Megjegyzések

- **Standalone komponensek:** Nincs NgModule, Angular 20 standalone API-kat használ
- **Signals:** Állapotkezelés Angular Signalokkal ahol alkalmazandó
- **Material Design 3:** Legújabb Material komponensek és témázás
- **OpenAPI-first:** Minden API kommunikáció generált klienseken keresztül
- **Type-safe:** Teljes TypeScript lefedettség strict moddal
- **Teszt lefedettség:** 88 unit teszt a core funkcionalitás lefedve
