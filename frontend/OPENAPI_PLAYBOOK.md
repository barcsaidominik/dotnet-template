# OpenAPI Generator Playbook (Frontend)

Ez a rövid útmutató azt írja le, hogyan dolgozzunk a backend OpenAPI specifikációból generált Angular klienssel.

## Forrás és cél

- Swagger forrás (frontendből nézve): `../backend/src/Template.Api/Template.Api.json`
- Típusok: `src/app/generated/api.types.ts`
- Generált Angular kliens: `src/app/generated/client/`

## Lokál fejlesztés (fontos)

- Frontend API base URL: `/api` (`environment.ts` és `environment.development.ts`)
- Angular dev proxy: `proxy.conf.json`
- Proxy target: `https://localhost:7133` (backend launchSettings szerint)
- Backend launchSettings példa: `https://localhost:7133;http://localhost:5279`

Megjegyzés: a dev proxy csak `ng serve` újraindítás után töltődik be.

## Napi workflow API változás után

1. Frissítsd a backend OpenAPI JSON-t.
2. Futtasd a teljes generálást:

```bash
npm run generate:api
```

3. Ellenőrizd a fordítást:

```bash
npm run build
```

4. Javítsd a feature kódot, ha típusváltozás történt.

## Fontos npm parancsok

- Teljes generálás:

```bash
npm run generate:api
```

- Csak típusok:

```bash
npm run generate:types
```

- Csak Angular kliens:

```bash
npm run generate:client
```

## Kötelező szabályok

- Ne szerkeszd kézzel a `src/app/generated/` tartalmát.
- API hívásra a generált service-eket használd a `src/app/generated/client/services/` alól.
- A generált service-ek Promise-t adnak vissza; RxJS flow esetén konvertálj `from(...)`-mal.
- A kliens root URL-t az app config állítja be (`provideApiConfiguration(environment.apiUrl)`).

## Review checklist (PR előtt)

1. Lefutott a `npm run generate:api`.
2. Lefutott a `npm run build`.
3. Nincs kézi módosítás a generált fájlokban.
4. A feature kód a generált service-eket hívja (nem kézi HttpClient endpoint stringeket).
5. Ha API contract változott, a releváns UI flow-k manuálisan kipróbálva.

## Gyors hibaelhárítás

- Hiba: nem találja a Swagger fájlt.
  - Ellenőrizd az útvonalat a `ng-openapi-gen.json` fájlban.
  - Ellenőrizd, hogy a backend oldali JSON valóban legenerálódott.

- Hiba: új mezők miatt TypeScript hiba.
  - Futtasd újra: `npm run generate:api`.
  - Igazítsd a komponens/service kódot az új modellekhez.

- Hiba: endpoint metódusnév megváltozott.
  - Nézd meg az aktuális generált service fájlt a `src/app/generated/client/services/` alatt.

## Ajánlott release előtti minimál ellenőrzés

1. `npm run generate:api`
2. `npm run build`
3. Bejelentkezés / jogosultság / fő CRUD flow gyors manuális ellenőrzése
