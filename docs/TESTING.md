# Testing

## Server

xUnit, FluentAssertions, NSubstitute, `WebApplicationFactory`, and Testcontainers MongoDB 7.

| Test | What it proves |
| --- | --- |
| Health live and ready | `/health/live` and `/health/ready` return Healthy JSON while MongoDB is up, and responses include `X-Content-Type-Options: nosniff` |
| Exception middleware | A thrown exception becomes ProblemDetails. The body has no exception message and no stack trace |

Run from the repository root:

```bash
dotnet test server/LostAndFound.sln
```

Docker should be running so the tests start a MongoDB 7 container via Testcontainers. When Docker is unavailable locally (outside CI), `ApiFactory` falls back to `Mongo2Go`.

Coverage is collected in CI with Coverlet (`XPlat Code Coverage`) and uploaded as the `server-coverage` artifact. A numeric coverage gate arrives in Phase 5.

## Client

Vitest, React Testing Library, and MSW (MSW is installed in Phase 0 and used when pages call the API in Phase 4).

```bash
cd client
npm run lint
npm run typecheck
npm test
npm run build
```

## CI

`.github/workflows/ci.yml` runs the server job and the client job on pull requests and on pushes to `main`.

## Privacy tests

The privacy suite is Phase 3. It will block CI when public payloads contain private fields or when a stored photo still has EXIF.
