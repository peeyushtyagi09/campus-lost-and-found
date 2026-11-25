# API

Base path for feature routes: `/api/v1`. JSON only. Errors use [RFC 7807](https://datatracker.ietf.org/doc/html/rfc7807) ProblemDetails and never include a stack trace.

Health routes are at the root, not under `/api/v1`.

## Implemented in Phase 0

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| GET | `/health/live` | none | Liveness. Does not check MongoDB. |
| GET | `/health/ready` | none | Readiness. Checks MongoDB. |

Both return JSON:

```json
{ "status": "Healthy", "checks": [{ "name": "self", "status": "Healthy" }] }
```

`/health/ready` returns 503 when MongoDB is unreachable. The status string is `Healthy` or `Unhealthy`.

Unhandled exceptions return `application/problem+json` with status 500, a generic title, and no exception message or stack trace.

## Implemented in Phase 1 (Auth)

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/v1/auth/register` | none | `{ displayName, email, password }` → 201 `{ user }` (409 neutral error on duplicate) |
| POST | `/api/v1/auth/login` | none | `{ email, password }` → 200 `{ accessToken, expiresAt, user }` (423 when locked out) |
| GET | `/api/v1/auth/me` | JWT | Current user `{ id, displayName, email, role }` |

## Implemented in Phase 2 (Lost Reports & Photos)

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/v1/lost-reports` | JWT | Create a lost report (encrypts private fields; ignores body `reporterId`) |
| GET | `/api/v1/lost-reports/mine?page=&pageSize=` | JWT | Owner's paginated reports with decrypted private fields |
| GET | `/api/v1/lost-reports/{id}` | JWT | Owner or Staff see full report; others get `404` |
| PUT | `/api/v1/lost-reports/{id}` | JWT owner | Update report (404 for non-owners) |
| DELETE | `/api/v1/lost-reports/{id}` | JWT owner/Staff | Soft close (`Status = Closed`, returns `204`) |
| POST | `/api/v1/uploads/photos` | JWT | Multipart image (`file`) → `201 { fileName, url }` (metadata stripped, re-encoded) |
| GET | `/api/v1/media/{fileName}` | none | Serves sanitized photo by GUID filename (`Cache-Control`, `nosniff`) |

## Implemented in Phase 3 (Public Board)

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| GET | `/api/v1/public/board?type=lost\|found&category=&area=&q=&page=&pageSize=` | none | Paginated `PublicReportDto` list (`Open` / `Matched` only, newest first, max `pageSize` 50) |

## Implemented in Phase 6 (Found Reports & Matching)

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/v1/found-reports` | JWT | Log a found item handed to campus desk (encrypts storage, contact, verification details) |
| GET | `/api/v1/found-reports/mine?page=&pageSize=` | JWT | Finder's paginated found reports |
| GET | `/api/v1/found-reports/{id}` | JWT | Finder or Staff see full details; others get `404` |
| PUT | `/api/v1/found-reports/{id}` | JWT | Finder or Staff update |
| DELETE | `/api/v1/found-reports/{id}` | JWT | Finder or Staff soft-close |
| GET | `/api/v1/matches/mine` | JWT | Possible matches for caller's lost reports (public found-item data only) |
| GET | `/api/v1/matches` | Staff | All matches |
| POST | `/api/v1/matches/run` | JWT | Recompute match suggestions on demand |
| POST | `/api/v1/matches/{id}/confirm` | JWT | Owner/Staff confirms match |
| POST | `/api/v1/matches/{id}/dismiss` | JWT | Owner/Staff dismisses match |

## Implemented in Phase 7 (Claims, Handover, Custody Record)

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| POST | `/api/v1/claims` | JWT | Submit claim with encrypted verification answers (records `ClaimSubmitted`) |
| GET | `/api/v1/claims/mine` | JWT | Claimant's own claims |
| POST | `/api/v1/claims/{id}/withdraw` | JWT | Withdraw pending claim |
| GET | `/api/v1/claims/review?status=` | Staff | Staff claim review queue with decrypted verification details |
| POST | `/api/v1/claims/{id}/approve` | Staff | Approve pending claim (`ClaimApproved`) |
| POST | `/api/v1/claims/{id}/reject` | Staff | Reject pending claim (`Rejected`) |
| POST | `/api/v1/handovers/{claimId}/complete` | Staff | Complete supervised handover (`witnessConfirmed && idChecked`, sets `Returned`, records `HandedOver`) |
| GET | `/api/v1/custody/{reportId}` | JWT | Custody timeline + hash-chain validity for finder, claimant, or Staff |

## Implemented in Phase 8 (Disposal, Notifications, Admin, Observability)

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| GET | `/api/v1/disposals/eligible` | Staff | Found items flagged after retention window |
| POST | `/api/v1/disposals/{foundReportId}/approve` | Staff | Approve disposal (`Donate`/`Recycle`/`Destroy`), delete stored photo, write `Disposed` custody event |
| GET | `/api/v1/notifications` | JWT | Caller's notifications |
| POST | `/api/v1/notifications/{id}/read` | JWT | Mark notification as read |
| GET | `/api/v1/admin/users` | Admin | List user accounts (id, displayName, role, createdAt) |
| POST | `/api/v1/admin/staff` | Admin | Create staff account (audited) |
| PATCH | `/api/v1/admin/users/{id}/role` | Admin | Update user role between `User` and `Staff` (audited) |
| GET | `/api/v1/admin/audit-logs?page=&pageSize=` | Admin | Read-only audit log |
| GET | `/metrics` | none | Prometheus-compatible request counter and uptime metrics |

The route table in the master prompt lists `/uploads/photos`, `/media/{fileName}`, and `/public/board` without the `/api/v1` prefix. This draft keeps feature routes under `/api/v1` and documents that choice in `docs/DECISIONS.md`. Media stays easy to proxy: nginx forwards `/api/` and `/media/`. The media path will be confirmed when uploads are built in Phase 2. If the implemented media route must be exactly `/media/{fileName}`, nginx already proxies that prefix.

Password policy: at least 10 characters with an uppercase letter, a lowercase letter, and a digit. Duplicate registration returns 409 with a message that does not reveal whether the email exists.

## Headers

Every response sets:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Content-Security-Policy: default-src 'none'`
- `Strict-Transport-Security` when the request is HTTPS and the environment is Production

## Limits

- Rate limiter: strict policy `auth` for `/api/v1/auth/*`, moderate policy `uploads` for photo uploads, plus a global fixed window.
- Request body limit: `Storage:MaxPhotoBytes` (default 5 MB).
- Allowed photo types, once uploads exist: JPEG, PNG, WebP.
- CORS allow-list from `Cors:AllowedOrigins`.
- Responses are compressed.
