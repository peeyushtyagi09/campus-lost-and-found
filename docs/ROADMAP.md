# Roadmap

Work stops at the end of each phase until the user says `next`.

| Phase | Milestone | Goal | Status |
| --- | --- | --- | --- |
| 0 | M0 | Solution, client shell, Docker, CI, docs, health checks, exception middleware | Done |
| 1 | M1-A | Field visibility, AES-256-GCM, user repository, register / login / me | Not started |
| 2 | M1-B | Lost reports and sanitized photo upload | Not started |
| 3 | M1-C | Public board, privacy test suite, dev seed | Not started |
| 4 | M1 | React client for auth, reports, and the board | Not started |
| 5 | M1 | Hardening, coverage gates, changelog, tag `v0.1.0` | Not started |
| 6 | M2 | Found reports and matching | Not started |
| 7 | M3 | Claims, supervised handover, custody hash chain | Not started |
| 8 | M4 | Disposal, notifications, production hardening, tag `v1.0.0` | Not started |

## Definition of done (every phase)

- Build has zero warnings.
- Tests pass, including privacy tests once they exist.
- No `any`, no commented-out code, no TODO in finished scope.
- Docs match the change.
- No secrets in the repo. `.env.example` is current.
- Docker Compose starts the stack from a clean clone.
- A short phase report is written to the user.
