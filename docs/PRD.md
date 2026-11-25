# Product requirements

## Problem

Students and staff lose items on campus and have no shared place to report them. Public posts leak phone numbers, email addresses, and identifying marks. When an item is returned, nobody records who handed it over, so disputes cannot be resolved.

## Product

Campus Lost & Found is a web application where people report lost or found items, receive match suggestions, and complete a verified, supervised handover with a custody record.

The design principle is privacy by design. Contact details, identifying marks, photo metadata, and owner-proof answers never appear on the public board.

## Users and roles

| Role | Can do |
| --- | --- |
| User (student or staff member) | Register, report lost items, see their own private details, claim items |
| Staff | Everything a user can do, plus review claims, supervise handovers, dispose of unclaimed items |
| Admin | Manage staff accounts and read the audit log |

## Milestones

| Milestone | Outcome | When |
| --- | --- | --- |
| M0 | Skeleton, CI, Docker, docs | Phase 0 (this draft) |
| M1 | Accounts, lost reports, sanitized photos, public board | Phases 1–5 |
| M2 | Found reports and match suggestions | Phase 6 |
| M3 | Claims, supervised handover, custody hash chain | Phase 7 |
| M4 | Disposal, notifications, production hardening | Phase 8 |

## M1 success criteria

- A person can register, sign in, and file a lost report with a photo.
- The public board shows title, category, coarse area, date, description, status, and the sanitized photo.
- The board does not show contact details, private notes, reporter id, or photo location metadata.
- The owner can see their own private fields. Another user receives 404, not 403.
- Login is rate limited and locks out after repeated failures.

## Out of scope until later milestones

Found reports, matching, claims, handovers, disposal, email notifications, and admin account management.
