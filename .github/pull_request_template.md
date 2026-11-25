## Summary

-

## Security & privacy checklist

- [ ] Entity properties have `[FieldVisibility(Visibility.Public|Private)]`
- [ ] Public DTOs use explicit allow-list mapping (no private fields or contact info)
- [ ] Private fields are encrypted at rest with AES-256-GCM
- [ ] Uploaded photos are sanitized (EXIF/IPTC/XMP/ICC stripped)
- [ ] No secrets, tokens, or passwords in code, tests, or logs

## Test plan

- [ ] `dotnet build server/LostAndFound.sln --configuration Release` (0 warnings)
- [ ] `dotnet test server/LostAndFound.sln` (all tests + privacy suite pass)
- [ ] `npm run lint`, `npm run typecheck`, `npm run test:coverage`, `npm run build` in `client/`
