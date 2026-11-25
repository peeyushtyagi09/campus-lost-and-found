# Campus Lost & Found

Students and staff report lost items, and later match, claim, and hand them over with a custody record. Private details never appear on the public board.

Phase 0 is the skeleton: health checks, security headers, Docker, CI, and docs. Reporting and accounts are not implemented yet.

## Prerequisites

- .NET 8 SDK (`global.json` pins `8.0.428` or a later 8.0 patch)
- Node.js 22
- Docker with Compose v2

## Quick start

```bash
cp .env.example .env
docker compose up --build
```

- API liveness: [http://localhost:8080/health/live](http://localhost:8080/health/live)
- API readiness: [http://localhost:8080/health/ready](http://localhost:8080/health/ready)
- Client: [http://localhost:8088](http://localhost:8088)
- Swagger (Development only): [http://localhost:8080/swagger](http://localhost:8080/swagger)

`docker-compose.override.yml` is loaded automatically. It publishes MongoDB on port 27017 and sets the API environment to Development.

Copy `.env.example` to `.env` before starting Compose. The example encryption key and JWT signing key are for a local machine only.

## Environment variables

| Variable | Purpose |
| --- | --- |
| `MONGO_CONNECTION_STRING` | MongoDB connection string. Inside Compose this is `mongodb://mongo:27017` |
| `MONGO_DATABASE_NAME` | Database name |
| `JWT_ISSUER`, `JWT_AUDIENCE` | Token issuer and audience |
| `JWT_SIGNING_KEY` | HS256 key, at least 32 bytes |
| `JWT_EXPIRY_MINUTES` | Access token lifetime |
| `ENCRYPTION_KEY` | Base64 AES-256 key (32 bytes) |
| `STORAGE_UPLOAD_PATH` | Directory for sanitized photos |
| `STORAGE_MAX_PHOTO_BYTES` | Maximum request body size (default 5242880) |
| `CORS_ALLOWED_ORIGIN` | Browser origin allowed to call the API |
| `RATE_LIMIT_*` | Fixed-window limits |
| `API_PORT`, `CLIENT_PORT` | Published host ports |
| `VITE_API_URL` | API base URL baked into the client. Empty uses the same origin |

For `dotnet run` on the host, point MongoDB at `mongodb://localhost:27017` and set the same values with ASP.NET Core's `__` nesting, for example `Mongo__ConnectionString` and `Encryption__Key`.

## Tests

```bash
dotnet test server/LostAndFound.sln
```

```bash
cd client
npm ci
npm run lint
npm run typecheck
npm test
npm run build
```

Server tests need a running Docker daemon (Testcontainers).

## Layout

See `AGENTS.md` and `docs/ARCHITECTURE.md`. The phase plan is in `docs/ROADMAP.md`.
