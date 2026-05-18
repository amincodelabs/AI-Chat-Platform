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

## Conversation Endpoints

Conversation endpoints require an authenticated cookie.

- `POST /api/conversations`
- `GET /api/conversations`
- `GET /api/conversations/{id}`
- `DELETE /api/conversations/{id}`
- `POST /api/conversations/{id}/messages`

Example local flow:

```bash
curl -c cookies.txt -X POST https://localhost:7078/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password1"}'

curl -b cookies.txt -X POST https://localhost:7078/api/conversations \
  -H "Content-Type: application/json" \
  -d '{"title":"First chat"}'

curl -b cookies.txt https://localhost:7078/api/conversations
```

## Blazor Web

Run the API first:

```bash
dotnet run --project src/PrivateAiChat.Api --launch-profile https
```

Then run the Blazor Web app:

```bash
dotnet run --project src/PrivateAiChat.Web --launch-profile https
```

Open `https://localhost:7135/login` or `https://localhost:7135/signup`.

The Blazor app reads its backend URL from `Api:BaseUrl` in `src/PrivateAiChat.Web/appsettings*.json`.
The default local API URL is `https://localhost:7078`.

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
