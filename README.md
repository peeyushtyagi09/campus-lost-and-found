# Campus Lost & Found Platform

Privacy-by-design web application where students and staff report lost or found items, receive match suggestions, and complete supervised handovers backed by an append-only custody hash chain. Private details (contacts, identifying marks, EXIF photo metadata) are encrypted at rest and never exposed on the public board.

## Tech Stack

- **Backend**: ASP.NET Core Web API (.NET 8, C# 12 controllers), FluentValidation, Serilog, SixLabors.ImageSharp, MailKit
- **Database**: MongoDB 7 (`MongoDB.Driver`)
- **Frontend**: React 18, TypeScript (strict), Vite, React Router, TanStack Query, React Hook Form + Zod
- **Testing**: xUnit, FluentAssertions, Testcontainers / Mongo2Go, Vitest, React Testing Library, MSW, `axe-core`
- **DevOps & Hosting**: Docker, Docker Compose, Nginx, GitHub Actions, Vercel (client), Render (API)

---

## How Docker Is Used Here

This repository uses **multi-stage Docker builds** to keep runtime images small and non-root, **Docker Compose** to run the 3-container stack locally or in production, and **Testcontainers** in CI integration tests.

### 1. Dockerfiles

| File | Build Context | Stages | Purpose |
| --- | --- | --- | --- |
| [`Dockerfile`](Dockerfile) | `.` (repo root) | `mcr.microsoft.com/dotnet/sdk:8.0` → `aspnet:8.0` | Cloud/Render API image (`render.yaml`). Restores and publishes `LostAndFound.Api.csproj`, runs as non-root `$APP_UID`, creates `/app/uploads`, and binds dynamically to `${PORT:-8080}`. |
| [`server/src/LostAndFound.Api/Dockerfile`](server/src/LostAndFound.Api/Dockerfile) | `./server` | `mcr.microsoft.com/dotnet/sdk:8.0` → `aspnet:8.0` | Docker Compose API image (`docker-compose.yml` & `docker-compose.prod.yml`). Identical runtime setup scoped to `server/`. |
| [`client/Dockerfile`](client/Dockerfile) | `./client` | `node:22-alpine` → `nginx:1.27-alpine` | Installs `npm ci`, bakes `VITE_API_URL` into `dist/` via `npm run build`, and serves static assets via Nginx on port `80`. |

`.dockerignore` (at root), `server/.dockerignore`, and `client/.dockerignore` keep `bin/`, `obj/`, `node_modules/`, `dist/`, `uploads/`, `.git/`, and local `.env` files out of build contexts.

### 2. Docker Compose Architecture

```text
Browser (:8088 or :80)
   │
   ▼
┌──────────────────────────────────────────┐
│ client (nginx:1.27-alpine)               │
│  ├─ /          -> /usr/share/nginx/html  │  (SPA fallback -> /index.html)
│  ├─ /api/      -> http://api:8080/api/   │
│  ├─ /health/   -> http://api:8080/health/│
│  └─ /media/    -> http://api:8080/media/ │
└────────────────────┬─────────────────────┘
                     │ Docker internal network
                     ▼
┌──────────────────────────────────────────┐
│ api (.NET 8 Web API, :8080)              │
│  ├─ Health checks: /health/live, /ready  │
│  └─ Volume: uploads_data -> /app/uploads │
└────────────────────┬─────────────────────┘
                     │ mongodb://mongo:27017
                     ▼
┌──────────────────────────────────────────┐
│ mongo (mongo:7, :27017)                  │
│  ├─ Healthcheck: mongosh ping            │
│  └─ Volume: mongo_data -> /data/db       │
└──────────────────────────────────────────┘
```

- **`docker-compose.yml`**: Base 3-service stack (`mongo`, `api`, `client`) with `mongo_data` and `uploads_data` named volumes. `api` waits for `mongo` (`condition: service_healthy`). `nginx.conf` proxies `/api/`, `/health/`, and `/media/` to `api:8080` so the browser uses a single origin.
- **`docker-compose.override.yml`**: Automatically loaded by `docker compose up` for local dev. Exposes MongoDB on host port `27017` and sets `ASPNETCORE_ENVIRONMENT=Development` (enabling Swagger at `/swagger`).
- **`docker-compose.prod.yml`**: Hardened production stack (`restart: always`, `read_only: true` containers with `tmpfs` mounts for `/tmp`, `/var/cache/nginx`, `/var/run`, explicit CPU/memory limits, retention & SMTP settings, client exposed on `CLIENT_PORT:-80`).

### 3. Docker in Integration Tests (Testcontainers)

`server/tests/LostAndFound.Api.Tests/Infrastructure/ApiFactory.cs` uses `Testcontainers.MongoDb` (`mongo:7`) whenever Docker is available (including GitHub Actions CI) and automatically falls back to `Mongo2Go` when running tests locally without Docker Desktop.

---

## Project File Structure

```text
.
├── .dockerignore                  # Excludes build outputs, uploads, .git, and .env from root Docker context
├── .env.example                   # Template environment variables for Docker Compose
├── .gitignore                     # Git ignore rules (.env, bin/, obj/, node_modules/, uploads/, .mongo2go/)
├── AGENTS.md                      # Repository engineering and privacy guardrails
├── CHANGELOG.md                   # Release history (v0.1.0, v1.0.0)
├── Dockerfile                     # Root API Dockerfile used by Render
├── docker-compose.yml             # Base MongoDB + API + Nginx client stack
├── docker-compose.override.yml    # Local dev overrides (exposes 27017, Development mode)
├── docker-compose.prod.yml        # Production Compose stack (read-only containers, resource limits)
├── global.json                    # Pins .NET 8 SDK version
├── README.md                      # Project, Docker, and structure guide
├── render.yaml                    # Render Blueprint spec for deploying the API Docker service
│
├── .github/
│   ├── CODEOWNERS                 # Code ownership rules
│   ├── dependabot.yml             # NuGet, npm, and GitHub Actions update checks
│   ├── pull_request_template.md   # PR checklist
│   └── workflows/ci.yml           # CI pipeline: server build/test/coverage + client lint/typecheck/test/coverage
│
├── docs/                          # Architecture and spec documentation
│   ├── API.md                     # REST endpoints, status codes, and payload contracts
│   ├── ARCHITECTURE.md            # Layering, background queues, and privacy boundaries
│   ├── DATA_MODEL.md              # Mongo entities with Public/Private field classifications
│   ├── DECISIONS.md               # Architectural & security decision records (D1–D18)
│   ├── PRD.md                     # Product requirements
│   ├── ROADMAP.md                 # Implementation phases (Phases 0–8)
│   ├── SECURITY.md                # Encryption, EXIF stripping, rate limits, and audit notes
│   └── TESTING.md                 # Test strategy and coverage targets
│
├── client/                        # React 18 + Vite + TypeScript frontend
│   ├── Dockerfile                 # Multi-stage Node 22 build -> Nginx 1.27 runtime
│   ├── nginx.conf                 # Reverse proxy (/api, /health, /media) + SPA try_files
│   ├── vercel.json                # Vercel Vite config, SPA rewrites, and security headers
│   ├── package.json               # Dependencies and scripts (lint, typecheck, test, build)
│   ├── tsconfig.json              # TypeScript strict configuration
│   ├── vite.config.ts             # Vite dev proxy (to localhost:8080) & Vitest coverage config
│   ├── public/favicon.svg
│   └── src/
│       ├── api/                   # Typed fetch wrappers (auth, board, lostReports, foundReports, matches, claims, custody, disposals, notifications, admin, uploads, http)
│       ├── assets/categories/     # Local SVG preview artwork for all 9 item categories
│       ├── components/            # Shared UI (Layout, ItemCard, ItemDetailModal, CustodyTimeline)
│       ├── hooks/                 # AuthContext and useAuth hook
│       ├── pages/                 # Route views (PublicBoardPage, ReportLostPage, ReportFoundPage, MyReportsPage, MatchesPage, ClaimSubmitPage, MyClaimsPage, StaffDashboardPage, CustodyPage, NotificationsPage, AdminPage, LoginPage, RegisterPage)
│       ├── routes/                # AppRouter and ProtectedRoute
│       ├── test/                  # Vitest + RTL + axe-core tests and MSW mock handlers
│       ├── types/                 # Shared TypeScript interfaces and enums
│       └── utils/                 # Photo category resolver and Zod/file validation helpers
│
└── server/                        # .NET 8 solution & backend
    ├── LostAndFound.sln           # Solution file linking API, seed CLI, and test project
    ├── .dockerignore              # Server build context ignores
    ├── scripts/seed/              # Dev seed CLI (1 admin, 1 staff, 3 users, 20 sample lost reports, 3 sanitized photos; blocks Production)
    ├── src/LostAndFound.Api/
    │   ├── Dockerfile             # Server-context Dockerfile for Docker Compose
    │   ├── Program.cs             # DI wiring, JWT auth, CORS, rate limiting, Swagger, health checks, /metrics
    │   ├── appsettings.json       # Base configuration sections
    │   ├── Controllers/           # Thin HTTP controllers (Auth, PublicBoard, LostReports, FoundReports, Matches, Claims, Handovers, Custody, Disposals, Notifications, Admin, Uploads, Media)
    │   ├── Dtos/                  # Allow-listed public & owner request/response DTOs
    │   ├── Entities/              # MongoDB documents & [FieldVisibility] markers
    │   ├── Middleware/            # RFC 7807 ProblemDetails exception handler, security headers, FluentValidation filter
    │   ├── Options/               # Validated IOptions<T> configs (Mongo, Jwt, Encryption, Storage, Cors, RateLimit, Matching, Retention, Email)
    │   ├── Privacy/               # AES-256-GCM encryption service, HMAC email hasher, [FieldVisibility]
    │   ├── Repositories/          # MongoContext, index creation, and MongoDB repositories
    │   ├── Services/              # Business logic, ImageSharp EXIF stripper, matching queue/engine, custody hash chain, retention job, MailKit/console email
    │   └── Validators/            # FluentValidation request validators
    └── tests/LostAndFound.Api.Tests/
        ├── Infrastructure/        # WebApplicationFactory + Testcontainers/Mongo2Go harness
        ├── Privacy/               # FieldVisibility, AES-256-GCM, and public-board leak/EXIF tests
        ├── Auth/, Reports/, Uploads/, Matching/, Claims/, Admin/, EndToEnd/
        └── HealthEndpointTests.cs, ExceptionHandlingMiddlewareTests.cs
```

---

## Prerequisites

- **.NET 8 SDK** (`global.json` pins `8.0.428` or compatible 8.0 patch)
- **Node.js 22** (or 20+ for local Vite development)
- **Docker with Compose v2** (for full container stack and Testcontainers)

---

## Quick Start (Docker Compose)

```bash
cp .env.example .env
docker compose up --build
```

- **Web Client**: [http://localhost:8088](http://localhost:8088)
- **API Liveness / Readiness**: [http://localhost:8080/health/live](http://localhost:8080/health/live) · [http://localhost:8080/health/ready](http://localhost:8080/health/ready)
- **Swagger UI** (Development): [http://localhost:8080/swagger](http://localhost:8080/swagger)

For production Compose:

```bash
docker compose -f docker-compose.prod.yml --env-file .env up --build -d
```

*(Replace the example `JWT_SIGNING_KEY` and `ENCRYPTION_KEY` in `.env` before any shared or production deployment.)*

---

## Running Locally Without Docker Compose

Point `Mongo__ConnectionString` at a local MongoDB instance (`mongodb://127.0.0.1:27017`) and configure `Jwt__SigningKey` and `Encryption__Key`:

```bash
# Seed development data (non-Production only)
dotnet run --project server/scripts/seed/LostAndFound.Seed.csproj

# Start API on http://localhost:8080
dotnet run --project server/src/LostAndFound.Api/LostAndFound.Api.csproj

# Start React dev server on http://localhost:5173 (proxies /api, /health, /media -> :8080)
cd client && npm ci && npm run dev
```

---

## Tests & Quality Checks

```bash
# Server (Release build + xUnit + privacy tests)
dotnet build server/LostAndFound.sln --configuration Release
dotnet test server/LostAndFound.sln --configuration Release

# Client (Prettier, ESLint, TypeScript strict check, Vitest + axe-core coverage, Vite build)
cd client
npm ci
npm run format:check
npm run lint
npm run typecheck
npm run test:coverage
npm run build
```

---

## Cloud Deployment (Vercel + Render)

- **Frontend (Vercel)**: Import the repo with **Root Directory** = `client` and **Framework Preset** = `Vite` (configured in [`client/vercel.json`](client/vercel.json)). Set `VITE_API_URL` to your deployed API URL and redeploy.
- **Backend (Render)**: Deploy using [`render.yaml`](render.yaml) (Blueprint) or a Docker Web Service using the root [`Dockerfile`](Dockerfile) with a MongoDB Atlas `Mongo__ConnectionString`, a 32-byte Base64 `Encryption__Key`, and `Cors__AllowedOrigins__0` set to your Vercel origin.
