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

## Health Checks

The API exposes three health endpoints:

- `GET /health` - readiness summary for the API and its dependencies
- `GET /health/ready` - readiness probe for database, Redis, and Ollama
- `GET /health/live` - liveness probe for the API process itself

Readiness returns a JSON document with the overall status, per-check status, duration, and request ID. Liveness stays independent of external dependencies so it can be used for container restarts and basic uptime checks.

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

Posting a message now saves both the user message and an assistant reply from Ollama:

```bash
curl -b cookies.txt -X POST https://localhost:7078/api/conversations/{conversationId}/messages \
  -H "Content-Type: application/json" \
  -d '{"content":"Give me a one sentence project status update."}'
```

## Ollama

The API reads Ollama settings from `src/PrivateAiChat.Api/appsettings*.json`:

```json
"Ollama": {
  "BaseUrl": "http://localhost:11434",
  "Model": "llama3.2",
  "TimeoutSeconds": 120
}
```

Local setup:

```bash
ollama serve
ollama pull llama3.2
```

## Rate Limiting

The API uses named ASP.NET Core rate-limiting policies configured from `RateLimiting`:

```json
"RateLimiting": {
  "Auth": {
    "PermitLimit": 5,
    "WindowSeconds": 60
  },
  "Chat": {
    "PermitLimit": 20,
    "WindowSeconds": 60
  },
  "General": {
    "PermitLimit": 120,
    "WindowSeconds": 60
  }
}
```

Auth endpoints use the strict `Auth` policy, message creation and streaming use `Chat`, and conversation reads/deletes use `General`. Health checks are not rate-limited.

The current implementation uses ASP.NET Core's in-process fixed-window limiter. It is safe for a single API instance and keeps named policy boundaries so a Redis-backed distributed limiter can replace it later for multiple API replicas.

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

## Local Docker

The local Docker setup runs:

- API
- Blazor Web
- SQL Server
- Redis
- Ollama

Copy the sample environment file if you want to override local ports or secrets:

```bash
cp .env.example .env
```

Start the stack:

```bash
docker compose up --build
```

Open the Blazor app:

```text
http://localhost:5080
```

The API is available for local debugging at:

```text
http://localhost:5081
```

SQL Server is bound to localhost only on port `14333` by default. Redis and Ollama are only available inside the Docker network. The API applies EF Core migrations automatically in Docker because `Database__ApplyMigrations=true` is set by `docker-compose.yml`.

Pull the configured Ollama model into the persistent Ollama volume:

```bash
docker compose exec ollama ollama pull llama3.2:1b
```

The Docker default uses `llama3.2:1b` so the full stack can run more comfortably on local development machines. Override `OLLAMA_MODEL` in `.env` if you want a larger model and have enough Docker memory available.

For local Docker, the API receives these container-specific settings from `docker-compose.yml`:

```text
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;...
ConnectionStrings__Redis=redis:6379
Database__ApplyMigrations=true
Ollama__BaseUrl=http://ollama:11434
Ollama__TimeoutSeconds=120
RateLimiting__Auth__PermitLimit=5
RateLimiting__Auth__WindowSeconds=60
RateLimiting__Chat__PermitLimit=20
RateLimiting__Chat__WindowSeconds=60
RateLimiting__General__PermitLimit=120
RateLimiting__General__WindowSeconds=60
Authentication__CookieSecurePolicy=SameAsRequest
```

The Blazor app reads its backend URL from `Api:BaseUrl` in `src/PrivateAiChat.Web/appsettings*.json`.
The default local API URL is `https://localhost:7078`.

## Production Docker

For a production-style container stack on a single host, use the production compose file:

```bash
cp .env.example .env
# edit .env with production values before starting
docker compose -f docker-compose.prod.yml up --build -d
```

## Logs

The API writes structured request logs that include:

- request path
- status code
- duration
- correlation ID
- authenticated user ID when available

For local Docker, follow the API logs with:

```bash
docker compose logs -f api
```

Health checks and error responses also include the correlation/request ID so you can tie a failure back to a specific request without exposing stack traces or sensitive data.

Production compose keeps only Nginx publicly exposed on port `80`. The API, web app, SQL Server, Redis, and Ollama stay on the internal Docker network.

Required production values in `.env`:

- `PRIVATEAICHAT_SQL_PASSWORD`
- `PRIVATEAICHAT_SQL_PID`
- `PRIVATEAICHAT_APPLY_MIGRATIONS`
- `PRIVATEAICHAT_AUTH_COOKIE_SECURE_POLICY`
- `OLLAMA_MODEL`

Deployment notes:

- `PRIVATEAICHAT_APPLY_MIGRATIONS=false` is the safer default. Run EF Core migrations before first start or temporarily set it to `true` for initial provisioning.
- `PRIVATEAICHAT_SQL_PID=Express` is the default in the sample file. Use a licensed edition if your deployment requires it.
- HTTPS and certificate management are not configured yet. Keep `PRIVATEAICHAT_AUTH_COOKIE_SECURE_POLICY=SameAsRequest` until TLS is added.
- The API and Web apps can also take production-specific defaults from `appsettings.Production.json`, but secrets should stay in environment variables.

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
