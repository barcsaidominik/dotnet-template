# Frontend

Az alkalmazás frontendje Angular alapú admin- és üzemi felület, generált API klienssel, többnyelvűséggel, sötét móddal és szigorú code style workflow-val.

## Stack

- `Angular 20`
- `Angular Material`
- `RxJS`
- `@ngx-translate`
- `Vitest`
- `ESLint`
- `Prettier`
- `ng-openapi-gen`

## Mit tud a frontend?

- login / regisztráció / jelszóbeállítás
- admin users oldal
- admin facilities oldal
- facility users oldal
- facility products oldal
- public products oldal
- mailbox oldal és unread badge
- magyar / angol nyelvváltás
- dark mode
- Excel export / import UI
- PDF letöltés

## Fejlesztői workflow

### Telepítés

```powershell
npm install
```

### Fejlesztői szerver

```powershell
npm start
```

Lokálisan a frontend a [proxy.conf.json](proxy.conf.json) alapján a gateway felé proxyz.

### Build

```powershell
npm run build
```

### Tesztek

```powershell
npm run test:run
```

### Style ellenőrzés

```powershell
npm run style:check
```

## API kliens generálás

A frontend nem kézzel karbantartott HTTP rétegre támaszkodik, hanem a backend OpenAPI leírásaiból generálja az API klienst.

Teljes újragenerálás:

```powershell
npm run generate:api
```

Kapcsolódó fájlok:
- [OPENAPI_PLAYBOOK.md](OPENAPI_PLAYBOOK.md)
- [ng-openapi-gen.json](ng-openapi-gen.json)
- [ng-openapi-gen-products.json](ng-openapi-gen-products.json)

Generált kód helye:
- `src/app/generated`

## Fontos scriptek

- `npm start`: fejlesztői szerver
- `npm run build`: production build
- `npm run test:run`: Vitest futtatás
- `npm run lint`: ESLint
- `npm run lint:fix`: automatikus javítás
- `npm run format`: Prettier formázás
- `npm run format:check`: formázás ellenőrzés
- `npm run style:check`: teljes style gate
- `npm run generate:api`: backend API kliens generálás

## Code style és git hook

A frontendhez külön szigorú styling workflow van bevezetve:
- [.editorconfig](.editorconfig)
- [eslint.config.js](eslint.config.js)
- [.prettierrc.json](.prettierrc.json)

Commit előtt a `simple-git-hooks` + `lint-staged` automatikusan futtatja a szükséges ellenőrzéseket a staged fájlokon.

## Fő route-ok

- `/auth/login`
- `/auth/register`
- `/auth/set-password`
- `/admin/users`
- `/admin/facilities`
- `/facility/users`
- `/facility/products`
- `/products`
- `/mailbox`

A route definíciók itt találhatók:
- [app.routes.ts](src/app/app.routes.ts)

## Többnyelvűség és megjelenés

### I18n

A projekt jelenleg magyar és angol nyelvet támogat.

Fordítási fájlok:
- [hu.json](src/assets/i18n/hu.json)
- [en.json](src/assets/i18n/en.json)

### Theme

A felület támogat világos és sötét megjelenést is, a fő shell komponensből vezérelve.

## Tesztelés

A frontend tesztcsomag a stabil üzleti és UI logikákra fókuszál:
- auth service és guardok
- i18n
- mailbox
- shell
- products és facility products
- admin users és admin facilities
- facility users
- util és mapper réteg

Jelenlegi állapot:
- `88/88` zöld

## Mikor jó ez a frontend alap?

Ez a frontend akkor erős kiindulópont, ha:
- Angular + Material adminfelületet szeretnél gyorsan indítani,
- fontos a generált API kliens,
- kell többnyelvűség és sötét mód,
- és szeretnél rögtön egy tesztelhető, formázásban fegyelmezett alapot.
