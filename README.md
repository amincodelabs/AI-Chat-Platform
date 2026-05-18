# PrivateAiChat

PrivateAiChat is a production-minded personal AI chat platform built with:

- .NET 9 Web API
- Blazor Web frontend
- SQL Server
- EF Core
- Clean Architecture

## Projects

- `PrivateAiChat.Api` - backend API and health endpoint
- `PrivateAiChat.Domain` - core entities and domain rules
- `PrivateAiChat.Application` - application layer
- `PrivateAiChat.Infrastructure` - EF Core and persistence
- `PrivateAiChat.Contracts` - shared contracts

## Initial Domain Model

- `User`
- `Conversation`
- `Message`

Common fields are modeled on a shared base entity:

- `Id`
- `CreatedAt`
- `UpdatedAt`
- `DeletedAt`
- `IsDeleted`

## Health Endpoint

`GET /health`

Returns API status and checks database connectivity.

## Auth Endpoints

Authentication uses ASP.NET Core Identity with secure password hashing and application cookies.

- `POST /auth/signup`
- `POST /auth/login`
- `POST /auth/logout`

Current password policy:

- Minimum 8 characters
- Requires a digit
- Requires lowercase
- Requires uppercase
- Does not require a symbol

## Migration

Create the initial EF Core migration:

```bash
dotnet ef migrations add InitialCreate \
  --project src/PrivateAiChat.Infrastructure \
  --startup-project src/PrivateAiChat.Api \
  --output-dir Persistence/Migrations
```

Apply the migration manually:

```bash
dotnet ef database update \
  --project src/PrivateAiChat.Infrastructure \
  --startup-project src/PrivateAiChat.Api
```

After the authentication foundation migration, update the database with the same `dotnet ef database update` command.
