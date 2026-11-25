# Security

## Privacy by design

- Public board DTOs are built from an allow-list (`ReportMappings.ToPublicDto`). They never map everything and strip fields afterward.
- Every entity property is classified `[FieldVisibility(Visibility.Public|Private)]` and enforced by `FieldVisibilityTests.EveryEntityPropertyIsClassified`.
- Private fields (`Email`, `PrivateDetails`, `ContactPhone`, `ContactEmail`) are encrypted at rest with AES-256-GCM (`AesGcmEncryptionService`), using a 32-byte key from `Encryption:Key` and a random 12-byte nonce per value (`base64(nonce|ciphertext|tag)`).
- Email lookup uses a keyed HMAC-SHA256 hash (`HmacEmailHasher`) derived from `Encryption:Key` via HKDF.
- Uploaded photos are decoded with ImageSharp, validated against JPEG/PNG/WebP and 8000x8000 px bounds, auto-oriented, stripped of EXIF/IPTC/XMP/ICC/CICP profiles, resized to max 1600 px, re-encoded, and stored under a random GUID filename.
- Passwords are hashed with ASP.NET Core Identity `PasswordHasher<User>`, never logged, and never returned. Five consecutive failures lock an account for 15 minutes (`423 Locked`).

## OWASP ASVS L1 controls

| Control | Implementation |
| --- | --- |
| Options validated on startup | `ValidateOnStart` on `MongoOptions`, `JwtOptions`, `EncryptionOptions`, `StorageOptions`, `CorsOptions`, `RateLimitOptions` |
| Security headers | `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'none'`, HSTS on HTTPS in Production |
| CORS allow-list | Configured via `Cors:AllowedOrigins`; wildcard `*` rejected at startup |
| Rate limiting | Fixed-window limits (`auth`, `uploads`, and global limiter) returning 429 RFC 7807 `ProblemDetails` |
| Body size limit | Kestrel + FormOptions + `UploadsController` enforce `Storage:MaxPhotoBytes` (5 MB default) |
| Error handling | `ExceptionHandlingMiddleware` returns RFC 7807 `ProblemDetails` with no exception message or stack trace |
| Input validation | FluentValidation via `FluentValidationActionFilter` (server) and Zod (client); contact info rejected in public report fields |
| Authorization | JWT HS256 (60-min lifetime, 1-min clock skew), `RequireStaff`, `RequireAdmin`, and 404 (not 403) on non-owner lost report access |
| Container hardening | API image runs as non-root `$APP_UID` |

## Dependency audit (Phase 5)

- `dotnet list server/LostAndFound.sln package --vulnerable --include-transitive`:
  - `SixLabors.ImageSharp` `3.1.12` has 6 documented 3.1.x advisories suppressed via `NuGetAuditSuppress` because `3.2+`/`4.x` require a build-time `SixLaborsLicenseKey` in Release builds (see `docs/DECISIONS.md` D14); mitigated by 5 MB cap, format allow-list (JPEG/PNG/WebP), and 8000x8000 dimension checks prior to pixel decoding.
  - `Testcontainers.MongoDb` updated to `4.17.0` (test-only dependency).
- `npm --prefix client audit --omit=dev`:
  - `react-router-dom` 6.x (`GHSA-wrjc-x8rr-h8h6`, `GHSA-337j-9hxr-rhxg`: SSR hydration/open-redirect advisories not triggered in this SPA without SSR; upgrade path to v7 tracked via Dependabot) and `zod` (`GHSA-mrhx-c7xc-m3w5`: oversized collection DoS in client validation; bounded by server validation and max input lengths).
