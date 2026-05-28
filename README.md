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
- `POST /api/conversations/{id}/messages/stream`

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

The Blazor chat UI uses the streaming endpoint. While a response is streaming, the composer shows a stop button. Stopping cancels the browser-to-API request, the API request token, and the Ollama HTTP stream. The user message remains saved. If partial assistant text has already streamed, the UI keeps it visible and the backend best-effort saves that partial assistant message. Empty assistant messages are not saved.

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

For local non-Docker development, apply EF Core migrations before using auth or chat:

```bash
dotnet ef database update \
  --project src/PrivateAiChat.Infrastructure \
  --startup-project src/PrivateAiChat.Api
```

## Local Docker

The local Docker setup runs:

- API
- Blazor Web
- SQL Server
- Redis
- Ollama

Local Docker requires a `.env` file for secret values. Copy the sample file and replace placeholder passwords before starting:

```bash
cp .env.example .env
# edit PRIVATEAICHAT_SQL_PASSWORD and PRIVATEAICHAT_REDIS_PASSWORD
```

Start the stack:

```bash
docker compose up --build
```

Open the Blazor app:

```text
http://localhost
```

Nginx is the only public service in the local compose stack. It proxies:

- `/` to the Blazor Web app
- `/api` to the API
- `/auth` to the API
- `/health` to the API health endpoint

SQL Server, Redis, Ollama, API, and Web stay inside the Docker network. The API applies EF Core migrations automatically in the local Docker stack because `Database__ApplyMigrations=true` is set by `docker-compose.yml`. Production compose defaults this to `false`.

Pull the configured Ollama model into the persistent Ollama volume:

```bash
docker compose exec ollama ollama pull llama3.2:1b
```

The Docker default uses `llama3.2:1b` so the full stack can run more comfortably on local development machines. Override `OLLAMA_MODEL` in `.env` if you want a larger model and have enough Docker memory available.

For local Docker, the API receives these container-specific settings from `docker-compose.yml`:

```text
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;...
ConnectionStrings__Redis=redis:6379,password=${PRIVATEAICHAT_REDIS_PASSWORD},abortConnect=false
Database__ApplyMigrations=true
Redis__Required=true
DevelopmentSeed__Enabled=false
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
- `PRIVATEAICHAT_REDIS_PASSWORD`
- `PRIVATEAICHAT_CORS_ALLOWED_ORIGINS`
- `PRIVATEAICHAT_MAX_REQUEST_BODY_BYTES`
- `PRIVATEAICHAT_SQL_PID`
- `PRIVATEAICHAT_APPLY_MIGRATIONS`
- `PRIVATEAICHAT_ALLOW_PRODUCTION_AUTO_MIGRATIONS`
- `PRIVATEAICHAT_AUTH_COOKIE_SECURE_POLICY`
- `PRIVATEAICHAT_HSTS_ENABLED`
- `OLLAMA_MODEL`

Deployment notes:

- `PRIVATEAICHAT_APPLY_MIGRATIONS=false` is the safer default. Run EF Core migrations before first start.
- If you intentionally want the API to apply migrations in production, set both `PRIVATEAICHAT_APPLY_MIGRATIONS=true` and `PRIVATEAICHAT_ALLOW_PRODUCTION_AUTO_MIGRATIONS=true`.
- `PRIVATEAICHAT_SQL_PID=Express` is the default in the sample file. Use a licensed edition if your deployment requires it.
- HTTPS and certificate management are not configured yet. Keep `PRIVATEAICHAT_HSTS_ENABLED=false` until HTTPS is configured.
- Production cookies default to `PRIVATEAICHAT_AUTH_COOKIE_SECURE_POLICY=Always`; use HTTPS before exposing the app publicly.
- The API and Web apps can also take production-specific defaults from `appsettings.Production.json`, but secrets should stay in environment variables.

## Security Configuration

Authentication uses ASP.NET Core Identity. Passwords are only passed to Identity APIs for signup/login, and password hashes are never returned by API contracts. Login failures use a generic unauthorized response. Duplicate signup attempts return a generic signup failure message to reduce account enumeration.

Conversation authorization is enforced by user-scoped repository queries. Get, rename, delete, non-streaming send, and streaming send operations all load conversations by both `UserId` and `ConversationId`, so users cannot access another user's conversations through the API.

The reverse proxy and API apply security headers:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy` denying camera, microphone, geolocation, payment, and USB
- `Content-Security-Policy` with `frame-ancestors 'none'`

HSTS is intentionally opt-in because this repo does not configure HTTPS yet. Enable it only after TLS is active at the public edge:

```text
PRIVATEAICHAT_HSTS_ENABLED=true
```

CORS defaults to no cross-origin access. If you deploy API and Web on different origins, set a comma-separated HTTPS origin list:

```text
PRIVATEAICHAT_CORS_ALLOWED_ORIGINS=https://chat.example.com,https://admin.example.com
```

Do not use wildcard origins in production. Same-domain deployment behind Nginx usually does not require CORS.

Request body size is limited at both Nginx and Kestrel. The default API body limit is `1048576` bytes:

```text
PRIVATEAICHAT_MAX_REQUEST_BODY_BYTES=1048576
```

Message content is limited to 16,000 characters, conversation titles to 200 characters, auth email to 256 characters, and passwords to 128 characters at the contract validation layer.

Assistant Markdown rendering disables raw HTML, removes unsafe links, and marks external links with `rel="noopener noreferrer"`.

Secrets policy:

- Do not commit real `.env` files.
- Keep SQL passwords in `PRIVATEAICHAT_SQL_PASSWORD`.
- Keep Redis credentials in `PRIVATEAICHAT_REDIS_PASSWORD`.
- Keep production host/origin values in environment variables.
- Rotate exposed credentials immediately if they are ever committed.

Exposed ports policy:

- Local and production compose expose only Nginx publicly.
- API, Web, SQL Server, Redis, and Ollama stay on the internal Docker network.
- API and Web images run as the built-in non-root `app` user; compose also applies `no-new-privileges` where practical.
- Do not publish SQL Server, Redis, Ollama, API, or Web service ports directly on a production host.

## Database Migrations

Current migrations live in `src/PrivateAiChat.Infrastructure/Persistence/Migrations`:

- `InitialCreate`
- `AddIdentityAuth`

Create a new migration after changing entities or EF Fluent API configuration:

```bash
dotnet ef migrations add DescriptiveMigrationName \
  --project src/PrivateAiChat.Infrastructure \
  --startup-project src/PrivateAiChat.Api \
  --output-dir Persistence/Migrations
```

Review generated migration files before committing them. They should contain only the intended schema changes.

Apply migrations manually:

```bash
dotnet ef database update \
  --project src/PrivateAiChat.Infrastructure \
  --startup-project src/PrivateAiChat.Api
```

List migrations:

```bash
dotnet ef migrations list \
  --project src/PrivateAiChat.Infrastructure \
  --startup-project src/PrivateAiChat.Api
```

Check for pending model changes:

```bash
dotnet ef migrations has-pending-model-changes \
  --project src/PrivateAiChat.Infrastructure \
  --startup-project src/PrivateAiChat.Api
```

Reset a local non-Docker database:

```bash
dotnet ef database drop \
  --project src/PrivateAiChat.Infrastructure \
  --startup-project src/PrivateAiChat.Api

dotnet ef database update \
  --project src/PrivateAiChat.Infrastructure \
  --startup-project src/PrivateAiChat.Api
```

Reset the local Docker database volume:

```bash
docker compose down -v
docker compose up --build
```

This removes all local container data, including SQL Server data and Ollama models.

## Development Seed Data

Development seed data is opt-in and only runs when `ASPNETCORE_ENVIRONMENT=Development`.

Enable it for Docker by setting these values in `.env`. Use a local-only password that is not committed:

```text
PRIVATEAICHAT_SEED_DEV_DATA=true
PRIVATEAICHAT_SEED_USER_EMAIL=dev.user@privateaichat.local
PRIVATEAICHAT_SEED_USER_DISPLAY_NAME=Development User
PRIVATEAICHAT_SEED_USER_PASSWORD=<your-local-dev-password>
```

Enable it for non-Docker development with .NET User Secrets instead of committing local appsettings files:

```bash
dotnet user-secrets set "DevelopmentSeed:Enabled" "true" --project src/PrivateAiChat.Api
dotnet user-secrets set "DevelopmentSeed:TestUser:Email" "dev.user@privateaichat.local" --project src/PrivateAiChat.Api
dotnet user-secrets set "DevelopmentSeed:TestUser:DisplayName" "Development User" --project src/PrivateAiChat.Api
dotnet user-secrets set "DevelopmentSeed:TestUser:Password" "<your-local-dev-password>" --project src/PrivateAiChat.Api
```

When enabled, startup creates the test user if missing and adds sample conversations/messages only if that user has no conversations. It never runs outside Development and does not seed production data.

## Startup Configuration Validation

The API validates critical configuration at startup and fails fast with clear errors for:

- Missing `ConnectionStrings:DefaultConnection`
- LocalDB connection string used in Production
- Invalid or missing `Ollama:BaseUrl`
- Missing `Ollama:Model`
- Invalid `Ollama:TimeoutSeconds`
- Missing `ConnectionStrings:Redis` when `Redis:Required=true`
- Invalid or wildcard CORS origins
- Invalid `RequestLimits:MaxRequestBodyBytes`
- Production auto-migration without `Database:AllowProductionAutoMigrations=true`
- Development seed data enabled outside Development
