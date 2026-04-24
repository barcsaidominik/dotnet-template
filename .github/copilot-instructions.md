# GitHub Copilot Code Review Instructions

## Fókuszterületek

Ez a repository ASP.NET Core alapú, háromszolgáltatásos architektúrával (Template.Api, Template.Products.Api, Template.Gateway.Api). A review során az alábbi területekre fókuszálj különösen.

---

## Biztonsági szempontok (prioritás: magas)

### Autentikáció és autorizáció
- Minden `[ApiController]` osztályon legyen `[Authorize]` attribútum, kivéve ha explicit `[AllowAnonymous]` indokolja
- Az `UseAuthentication()` hívásnak **meg kell előznie** `UseAuthorization()`-t a `Program.cs` middleware-sorrendben
- JWT konfiguráció: `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ClockSkew = TimeSpan.Zero` kötelező
- Refresh token-ek tárolása előtt `HashToken()` (SHA-256) hívása szükséges

### Bemeneti validáció
- Minden Command/Query osztályhoz léteznie kell megfelelő `AbstractValidator<T>` implementációnak
- A validátor ne legyen üres: minden releváns property-re legyen szabály
- `FromSqlRaw` és `ExecuteSqlRaw` hívásokban tilos string interpoláció — csak paraméterezett lekérdezés elfogadható

### Titkos adatok kezelése
- Tilos `Secret`, `Password`, `ConnectionString` és `ApiKey` jellegű property-kbe alapértéket (literális stringet) írni
- Konfigurációs értékek kizárólag Docker Secrets (`/run/secrets/`) vagy environment variable forrásból jöhetnek

### Naplózás
- `LogInformation`, `LogWarning`, `LogError` hívásokban tilos jelszó, token, refresh token vagy egyéb hitelesítési adat megjelenítése
- Strukturált naplózás esetén a `{Password}`, `{Token}`, `{Secret}` placeholder-ek piros zászlót jelentenek

### Adathozzáférés
- A `FacilityProductGuard` és az `IQueryGuard<T>` implementációk elhagyása IDOR-sérülékenységet okoz — jelezd, ha query ezeket megkerüli
- Közvetlen `DbContext` hozzáférés service réteg megkerülésével gyanús

---

## Kódminőség szempontok (prioritás: közepes)

- Üres `catch(Exception) {}` blokk elfogadhatatlan — legalább naplózás szükséges
- `.Result` és `.Wait()` aszinkron kódban deadlock-kockázatot jelent
- `HttpClient` ne legyen `new`-val példányosítva — `IHttpClientFactory` a helyes megközelítés
- Minden `HttpClient` hívásban ellenőrizd, hogy az URL nem közvetlenül felhasználói bemenetből épül-e fel (SSRF kockázat)

---

## Amit NE jelezz

- Általános stílusproblémákat (ezeket a `style-check.yml` kezeli)
- Olyan dolgokat, amelyek a teljes kódbázis ismerete nélkül nem ítélhetők meg
- Harmadik féltől származó csomagok verzióproblémáit (ezeket a NuGet audit kezeli)

---

## Elvárt válaszformátum

- Minden jelzéshez add meg: **súlyosság** (Critical / High / Medium), **fájl és sor**, **mi a probléma**, **miért számít**, **javasolt javítás**
- Ha nincs érdemi biztonsági találat, mondd ki egyértelműen
- Zárd a review-t egy egysoros összefoglaló verdikttel
