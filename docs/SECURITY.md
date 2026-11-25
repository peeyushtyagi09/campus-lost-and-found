# Security

## Privacy by design

- Public board DTOs are built from an allow-list. They are not entities with fields removed.
- Private values are encrypted at rest (AES-256-GCM) once the field exists. Phase 0 stores no personal data.
- Photos will be decoded, stripped of EXIF, IPTC, XMP, and ICC, and stored under a GUID name. The original file is never kept.
- Passwords are hashed with `PasswordHasher<T>`, never logged, and never returned. Login will lock out after repeated failures.
- Logs must not contain private fields, tokens, or passwords. The exception middleware logs the server-side exception and returns a generic ProblemDetails body.

## Phase 0 controls

| Control | Status |
| --- | --- |
| Options validated on startup | In place. The process refuses to start without Mongo, JWT, and encryption settings |
| Security headers | In place, including HSTS on HTTPS in Production |
| CORS allow-list | In place. No wildcard origin |
| Rate limiting | Policies registered. Auth and upload routes will opt in when those controllers exist. A global limit is active |
| Body size limit | Kestrel and form limits follow `Storage:MaxPhotoBytes` |
| Exception details | ProblemDetails never includes the exception message or stack trace |
| Secrets in git | `.env` is ignored. `.env.example` holds obvious development placeholders |
| Container user | API image runs as the non-root `$APP_UID` user |
| Swagger | Development only |

## Authentication (Phase 1 — implemented)

JWT bearer access tokens, HS256, signing key at least 32 bytes, 60-minute lifetime, clock skew of one minute. Claims: `sub`, `role`, `jti`. Policies: `RequireStaff`, `RequireAdmin`. Passwords hashed with `PasswordHasher<User>`; 5 failed login attempts trigger a 15-minute lockout (`423 Locked`), reset on successful authentication. AES-256-GCM encryption (`AesGcmEncryptionService`) and keyed `EmailHash` (`HmacEmailHasher`) are active for `users`.

## Still open

Dependency audit gates, coverage thresholds, login lockout, encryption implementation, and the privacy test suite land in later phases. See `docs/ROADMAP.md`.

Production HTTPS redirection and a locked-down compose file are Milestone 4. Phase 0 serves HTTP inside the Docker network so health checks work without a certificate. HSTS is sent only on HTTPS requests.
