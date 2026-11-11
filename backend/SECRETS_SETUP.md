# Backend Secrets Setup

The backend now supports two primary configuration paths:

- local development: `.NET user-secrets`
- container/devops style runs: environment variables or `.env`

## Local Development

`Template.Api`, `Template.Products.Api` and `Template.Gateway.Api` share the same `UserSecretsId`, so one set of commands (run from the repository root) is enough.

Set the required values:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=template_db;Username=postgres;Password=your-db-password" --project backend/src/services/Template.Api/Template.Api.csproj
dotnet user-secrets set "JwtSettings:Secret" "replace-with-a-long-random-secret-at-least-32-characters" --project backend/src/services/Template.Api/Template.Api.csproj
dotnet user-secrets set "InternalServiceAuth:Token" "replace-with-a-long-random-internal-service-token" --project backend/src/services/Template.Api/Template.Api.csproj
dotnet user-secrets set "Frontend:BaseUrl" "http://localhost:4200" --project backend/src/services/Template.Api/Template.Api.csproj
```

Optional SMTP settings for non-file email delivery:

```powershell
dotnet user-secrets set "Email:SmtpHost" "smtp.example.com" --project backend/src/services/Template.Api/Template.Api.csproj
dotnet user-secrets set "Email:SmtpPort" "587" --project backend/src/services/Template.Api/Template.Api.csproj
dotnet user-secrets set "Email:From" "noreply@example.com" --project backend/src/services/Template.Api/Template.Api.csproj
dotnet user-secrets set "Email:Username" "smtp-user" --project backend/src/services/Template.Api/Template.Api.csproj
dotnet user-secrets set "Email:Password" "smtp-password" --project backend/src/services/Template.Api/Template.Api.csproj
```

## Docker Compose

Copy [.env.example](../.env.example) to `.env`, then adjust the values before starting compose.

```powershell
docker compose up -d --build
```

## Startup Validation

The applications now validate these settings on startup:

- `ConnectionStrings:DefaultConnection`
- `JwtSettings`
- `InternalServiceAuth`
- `Frontend:BaseUrl`
- `ProductsService`
- `Email` outside Development

Both backend hosts now fail fast when `JwtSettings:Secret` or `InternalServiceAuth:Token` are missing.
