# Changelog

All notable changes to the Campus Lost & Found Platform are documented in this file.

## [0.1.0] - 2026-11-26

### Added
- **Foundations (M0)**: .NET 8 Web API solution, Serilog, ProblemDetails exception middleware, security headers middleware, rate limiting, health checks (`/health/live`, `/health/ready`), MongoDB context + startup indexes, React 18 TypeScript client, Docker Compose, GitHub Actions CI, and architecture/security documentation.
- **Privacy Core & Authentication (M1-A)**: `[FieldVisibility]` entity classification with reflection enforcement, AES-256-GCM field encryption (`AesGcmEncryptionService`), HKDF-keyed `EmailHash` lookup, `PasswordHasher<User>`, 5-attempt / 15-minute account lockout, JWT bearer auth, and `RequireStaff`/`RequireAdmin` policies.
- **Lost Reports & Photo Sanitization (M1-B)**: `LostReport` entity with encrypted private fields, ImageSharp photo sanitizer (auto-orient, EXIF/IPTC/XMP/ICC removal, 8000px dimension guard, 1600px max resize, re-encoding), GUID-only `LocalFileStorage`, `/api/v1/uploads/photos`, `/api/v1/media/{fileName}`, and owner-isolated `/api/v1/lost-reports` CRUD.
- **Public Board & Privacy Suite (M1-C)**: `/api/v1/public/board` returning allow-listed `PublicReportDto` items, `PrivacyTests` suite, and idempotent dev seed CLI (`server/scripts/seed`).
- **React Client & Hardening (M1)**: Public board with filters/pagination, login/register pages, lost report submission with client-side photo validation and EXIF-removal notice, owner report management, end-to-end M1 acceptance tests, CI coverage gates (`>= 80%` server Services/Privacy, `>= 70%` client), Dependabot, and CODEOWNERS.
