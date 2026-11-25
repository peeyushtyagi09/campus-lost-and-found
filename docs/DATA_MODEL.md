# Data model

Visibility is part of the schema. Every entity property will carry `[FieldVisibility(Visibility.Public)]` or `[FieldVisibility(Visibility.Private)]` when the entity types are added (Phase 1). Public DTOs are allow-lists of Public fields only.

Phase 0 creates the `users` and `lost_reports` collections and their indexes. It does not insert documents and does not define entity classes yet. See `docs/DECISIONS.md`.

Collection names implemented: `users`, `lost_reports`, `found_reports`, `matches`, `claims`, `handovers`, `custody_events`, `disposals`, `notifications`, `audit_events`. Every entity property carries `[FieldVisibility(Visibility.Public|Private)]` enforced by reflection tests.

## users

| Field | Type | Visibility | Notes |
| --- | --- | --- | --- |
| Id | ObjectId | Public | |
| DisplayName | string (2–60) | Public | Board shows a first name or alias only |
| Email | string | Private | Unique, stored lowercase. Encrypted copy plus `EmailHash` (SHA-256 with a server pepper) for lookup |
| PasswordHash | string | Private | Never present in any DTO |
| Role | enum User / Staff / Admin | Private | |
| FailedLoginCount | int | Private | |
| LockoutEnd | date | Private | |
| CreatedAt | UTC date | Private | |

Index: unique `{ EmailHash: 1 }`.

## lost_reports

| Field | Type | Visibility | Notes |
| --- | --- | --- | --- |
| Id | ObjectId | Public | |
| ReporterId | ObjectId | Private | Taken from the JWT, never from the body |
| Title | string (3–100) | Public | |
| Category | enum Phone, Laptop, Wallet, ID Card, Keys, Bag, Clothing, Books, Other | Public | |
| PublicDescription | string (0–500) | Public | Validator rejects email addresses and phone numbers |
| CampusArea | enum / string | Public | Coarse location only |
| DateLost | date | Public | Date only, no time |
| PhotoFileName | string? | Public via URL only | Sanitized image |
| Status | enum Open, Matched, Claimed, Returned, Closed | Public | |
| PrivateDetails | string | Private | Encrypted. Serial numbers, unique marks, exact location |
| ContactPhone | string | Private | Encrypted |
| ContactEmail | string | Private | Encrypted |
| CreatedAt | UTC date | Public | Shown publicly as `PostedAt` on `PublicReportDto` |
| UpdatedAt | UTC date | Private | |

Indexes:

- `{ Status: 1, Category: 1, CreatedAt: -1 }`
- `{ ReporterId: 1, CreatedAt: -1 }`
- text index on `Title` and `PublicDescription`

## Encryption

Private string fields are AES-256-GCM. The key is `Encryption:Key` (base64, 32 bytes). Each value uses a random 12-byte nonce and is stored as `base64(nonce | ciphertext | tag)`. The service is implemented in Phase 1.
