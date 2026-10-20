# Sprintle

A RESTful API built with ASP.NET Core and Clean Architecture (API → Application → Domain ← Infrastructure).

## Prerequisites

- .NET 10.0 SDK
- PostgreSQL (or your database of choice)

## Quickstart

```powershell
dotnet restore
dotnet build
```

## Tests

```powershell
dotnet test
```

## Managing User Secrets

For local development, sensitive settings (such as connection strings, JWT keys, and API keys) are managed using the [.NET Secret Manager](https://learn.microsoft.com/aspnet/core/security/app-secrets) (`dotnet user-secrets`).

Projects configured with Secret Manager:
- `src/microservices/Tasks/Tasks.API`
- `src/microservices/Users/Users.API`
- `src/gateway`

You can configure secrets either from inside the project directory or by passing `--project <path-to-csproj>`.

### Option 1: Set Individual Secrets

Navigate to the project directory (or use `--project`):

```bash
cd src/microservices/Tasks/Tasks.API
dotnet user-secrets set "IntegrationApiKeyOptions:ApiKey" "your-secret-key"
```

Or target a specific project directly:

```bash
dotnet user-secrets set "IntegrationApiKeyOptions:ApiKey" "your-secret-key" --project src/microservices/Tasks/Tasks.API
```

### Option 2: Set Secrets in Bulk from JSON

You can prepare a JSON configuration file (e.g. `input.json`):

```json
{
  "ConnectionStrings": {
    "TasksDb": "Host=localhost;Database=TasksDb;Username=postgres;Password=postgres"
  },
  "JwtOptions": {
    "Key": "your-secret-key-at-least-32-characters-long",
    "Issuer": "Sprintle",
    "Audience": "SprintleClient",
    "TokenValidityMins": "60"
  },
  "IntegrationApiKeyOptions": {
    "ApiKey": "your-api-key"
  }
}
```

Pipe the JSON file into `dotnet user-secrets set`:

#### Linux or macOS
```bash
cat ./input.json | dotnet user-secrets set
```

#### Windows (PowerShell)
```powershell
Get-Content ./input.json | dotnet user-secrets set
```

#### Windows (Command Prompt)
```cmd
type .\input.json | dotnet user-secrets set
```

### Useful Secret Manager Commands

- **List existing secrets:**
  ```bash
  dotnet user-secrets list
  ```
- **Remove a secret:**
  ```bash
  dotnet user-secrets remove "IntegrationApiKeyOptions:ApiKey"
  ```
- **Clear all secrets for project:**
  ```bash
  dotnet user-secrets clear
  ```


## Project Structure

- `src/` — application source code (4 layers)
- `tests/` — unit & architecture tests

## Architecture

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, value objects, domain events, repository interfaces |
| **Application** | Use cases, commands/queries (MediatR), validators |
| **Infrastructure** | Database, external services, repository implementations |
| **API** | Controllers/endpoints, middleware, DI composition root |

## License

MIT — see [LICENSE](LICENSE).
