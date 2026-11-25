# Agent guide

This repository is the Campus Lost & Found Platform. Read `.cursor/rules/project.mdc` and `docs/ROADMAP.md` before changing code.

## How to work

- Implement one roadmap phase at a time. Do not start the next phase until the user says `next`.
- Before editing, list the files you will create or change.
- Finish a phase only when the Definition of Done in the master prompt is met: zero warnings, tests and linters green, docs updated, no secrets committed.
- Record security-sensitive choices in `docs/DECISIONS.md`.
- Use a `feature/<name>` branch and conventional commits. Never commit `.env` or real keys.

## Layout

- `server/` — ASP.NET Core Web API (.NET 8, controllers) and xUnit tests.
- `client/` — React 18 + Vite + TypeScript.
- `docs/` — product, architecture, data model, API, security, and test notes.
- `docker-compose.yml` — MongoDB 7, API, and nginx client.

## Privacy

Public responses are allow-lists. Private fields stay encrypted. Do not log passwords, tokens, or private fields. A failing privacy test blocks the build once those tests exist (Phase 3).
