# Decisions

Choices made where the spec allowed more than one secure, simple option.

## D1. Repository root is this workspace

The master prompt shows a folder named `campus-lost-and-found/`. The files live at the root of this repository so the open workspace is the project. The compose project name is `campus-lost-and-found`.

## D2. .NET 8 is pinned

`global.json` requires the .NET 8 SDK (`8.0.428`, roll forward within the 8.0 feature band). Later SDKs installed on the machine are not used to build this solution.

## D3. Health responses are JSON

The API is JSON only. `/health/live` and `/health/ready` return `{ "status", "checks" }` instead of the plain-text writer. A failure status code is still 503.

## D4. Exception bodies never include internal details

ProblemDetails titles are generic in every environment, including Development. The server log still records the exception for operators. This is stricter than the spec's production-only wording and avoids a Development build leaking messages.

## D5. Phase 0 collections are untyped documents

`MongoContext` exposes `IMongoCollection<BsonDocument>` for `users` and `lost_reports`, and the hosted service creates the indexes from Section 5. Entity classes arrive with `[FieldVisibility]` in Phase 1. Switching the collection type then does not require a new index.

## D6. EmailHash unique index is not sparse

MongoDB allows a single document that omits a uniquely indexed field. The application will always write `EmailHash` (Phase 1), so the index is unique and not sparse.

## D7. Feature routes will use `/api/v1`

Health stays at `/health/*`. Auth and lost-report routes will be under `/api/v1`. The master prompt's upload, media, and board paths are not prefixed; nginx proxies both `/api/` and `/media/`. The exact media and board paths will be fixed in the phase that implements them, and `docs/API.md` will be updated to match Swagger.

## D8. HSTS only on HTTPS

`UseHttpsRedirection` is deferred to Milestone 4. The security-headers middleware adds `Strict-Transport-Security` only when the environment is Production and the request is HTTPS, so HTTP health checks inside Docker still succeed.

## D9. Required secrets fail startup

`appsettings.json` does not contain signing or encryption keys. `Jwt:SigningKey` and `Encryption:Key` must be supplied by the environment. Docker Compose reads them from `.env`, which is git-ignored. `.env.example` documents the variable names and uses obvious development placeholders.

## D10. Client styling is deferred

Phase 0 ships the Vite default stylesheet and a placeholder route. CSS Modules or Tailwind will be chosen in Phase 4 and recorded here.

## D11. Starter pull request template

Section 4 lists `.github/pull_request_template.md`. A short template is added now. Phase 5 still adds Dependabot, CODEOWNERS, and any template sections that the hardening pass needs.

## D12. Rate-limit policies exist before the routes do

`auth` and `uploads` policies are registered in Phase 0 so later controllers can attach them without changing host defaults. A global fixed window is already enforced.

## D13. Mongo2Go fallback for local server tests when Docker is unavailable

Server integration tests use `Testcontainers.MongoDb` (`mongo:7`) whenever Docker is available (including in CI). If Docker is unavailable locally, `ApiFactory` falls back to `Mongo2Go` so the test suite can still run without weakening CI coverage.

## D14. ImageSharp 3.1.12 and client toolchain

`SixLabors.ImageSharp` is pinned to `3.1.12` (with `NuGetAuditSuppress` for its 6 known 3.1.x advisories) because ImageSharp `3.2.0+` / `4.x` requires a build-time `SixLaborsLicenseKey` in Release builds. `NuGetAudit` and `TreatWarningsAsErrors=true` remain active for all packages, and Phase 2 image decoding enforces a 5 MB body limit, format allow-list (JPEG/PNG/WebP), and dimension bounds before pixel decoding. Organizations with a Six Labors license key can upgrade to `3.2+`/`4.x`. The React 18 client uses Vite, TypeScript `strict: true`, ESLint flat config (`no-explicit-any: error`), Prettier, Vitest, React Testing Library, and MSW.

## D15. EmailHash pepper derived via HKDF

`EmailHash` uses HMAC-SHA256 over the trimmed, lowercased email with a 32-byte key derived from `Encryption:Key` via HKDF (`campus-lost-and-found:email-hash:v1`). This avoids requiring an extra environment variable while keeping lookup hashes keyed with a server secret.

## D16. Account lockout and strict RBAC policies

Five consecutive failed password verifications lock the account for 15 minutes (`423 Locked`), and any valid login resets `FailedLoginCount` and `LockoutEnd`. Authorization policies follow strict least privilege: `RequireStaff` requires `Staff`, and `RequireAdmin` requires `Admin`.

## D17. FluentValidation action filter

`FluentValidationActionFilter` validates controller request DTOs using registered `IValidator<T>` instances without using the deprecated `FluentValidation.AspNetCore` auto-validation adapter, returning RFC 7807 `ValidationProblemDetails` with zero compiler warnings.

