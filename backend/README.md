# Backend

A backend több projektre bontott .NET megoldás, amely egyszerre támogatja a klasszikus rétegzett felépítést és a fokozatos microservice irányba történő továbblépést.

## Stack

- `.NET 10`
- `ASP.NET Core`
- `Entity Framework Core`
- `PostgreSQL`
- `JWT` + refresh token
- `Mediator`
- `ErrorOr`
- `FluentValidation`
- `Serilog`
- `TickerQ`
- `QuestPDF`
- `ClosedXML`
- `gRPC`

## Solution szerkezet

A megoldást a [Template.slnx](Template.slnx) fogja össze.

### Common projektek
- `Template.Common`
- `Template.Common.Email`
- `Template.Common.Excel`
- `Template.Common.Jobs`
- `Template.Common.Pdf`
- `Template.Common.Templating`

Ezek a keresztmetszeti, újrafelhasználható építőelemeket adják.

### Core projektek
- `Template.Domain`
- `Template.Application`
- `Template.Infrastructure`
- `Template.Grpc`

Itt található a tényleges üzleti modell, a use case réteg, az infrastruktúra és a service-to-service szerződés.

### Service hostok
- `Template.Api`
- `Template.Gateway.Api`
- `Template.Products.Api`

## Szolgáltatások szerepe

### Template.Api

A core API host fő felelősségei:
- auth és user lifecycle
- admin végpontok
- facility user menedzsment
- mailbox
- háttérjob ütemezés
- middleware-ek és observability

Fő fájl: [Program.cs](src/services/Template.Api/Program.cs)

### Template.Products.Api

A products service külön hostként futtatható, és jelenleg:
- products REST végpontokat ad,
- gRPC végpontot biztosít más szolgáltatásoknak,
- elkülönített service boundary-ként viselkedik.

Fő fájl: [Program.cs](src/services/Template.Products.Api/Program.cs)

### Template.Gateway.Api

A gateway egy reverse proxy belépési pont, ami lokális és dockeres futtatásnál egységes front door szerepet ad.

Fő fájl: [Program.cs](src/services/Template.Gateway.Api/Program.cs)

## Fő backend képességek

- regisztráció, login, refresh token
- admin approval flow
- felhasználó- és üzemkezelés
- termékkezelés külön service-ben
- Excel import / export
- PDF generálás
- mailbox
- TickerQ háttérjobok
- request telemetry
- resource guard és load shedding
- internal service auth
- OpenAPI dokumentumok

## Biztonsági megközelítés

### Auth
- JWT bearer tokenek
- refresh token flow
- szerepkör alapú authorization

### Secrets és config

Typed settings osztályokkal történik a bindolás, nem laza stringes konfigurációval.

Főbb beállítások:
- adatbázis
- JWT
- frontend URL-ek
- email
- belső service token

Lásd:
- [SECRETS_SETUP.md](SECRETS_SETUP.md)
- [Template.Api appsettings.json](src/services/Template.Api/appsettings.json)
- [Template.Products.Api appsettings.json](src/services/Template.Products.Api/appsettings.json)

### Service-to-service auth

A gRPC kommunikáció belső tokennel védett, így a szolgáltatások közti hívások nem anonimak.

## Fejlesztői indítás

### Restore / build / test

```powershell
dotnet restore Template.slnx
dotnet build Template.slnx
dotnet test tests/Template.Tests/Template.Tests.csproj
```

### Közvetlen host futtatás

Core API:

```powershell
dotnet run --project src/services/Template.Api/Template.Api.csproj
```

Products API:

```powershell
dotnet run --project src/services/Template.Products.Api/Template.Products.Api.csproj
```

Gateway:

```powershell
dotnet run --project src/services/Template.Gateway.Api/Template.Gateway.Api.csproj
```

## OpenAPI és frontend kliens

A backend build elő tudja állítani az OpenAPI JSON fájlokat:
- [Api.json](Api.json)
- [ProductsApi.json](ProductsApi.json)

Ezekből a frontend generálja újra a kliensoldali típusrendszert és szolgáltatásokat.

## Docker

A backendhez több image is tartozik:
- [Dockerfile](Dockerfile)
- [Dockerfile.GatewayApi](Dockerfile.GatewayApi)
- [Dockerfile.ProductsApi](Dockerfile.ProductsApi)

Összerakásuk compose alatt történik a repository gyökeréből:

```powershell
docker compose up -d --build
```

## Megfigyelhetőség és stabilitás

A projektben már bent vannak azok az alapok, amelyek egy template-et production-közelivé tesznek:
- strukturált request telemetry
- érzékeny query paraméterek redakciója
- resource guard
- kooperatív cancellation kritikus terhelésnél
- háttérjobok és mailbox események

## Tesztek

A backend tesztcsomag több szintet fed:
- domain unit tesztek
- application unit tesztek
- infrastructure tesztek
- célzott integration tesztek

Jelenlegi állapot:
- `114/114` zöld

Tesztprojekt:
- [Template.Tests.csproj](tests/Template.Tests/Template.Tests.csproj)
