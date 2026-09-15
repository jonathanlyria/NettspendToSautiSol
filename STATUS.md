# Project status

Updated: 15 September 2026.

## Completed in the portfolio cleanup

- Documented architecture, environment setup, current limits, and the graph objective.
- Adopted total dissimilarity (`1 - similarity`) as the route cost.
- Fixed disconnected and missing-endpoint route handling; retained isolated artists when loading SQLite.
- Added a regression runner covering graph behaviour and database conversion, including an independent all-pairs comparison.
- Replaced committed credential literals and launch arguments with environment configuration, and removed credential/token logging in the main applications.
- Removed generated build output and restored dependency folders from version control; retained source, the included database, and the original report.
- Added a build/check workflow. Its hosted run remains to be verified after publication.

## Verification

- Original solution compiled locally with 22 warnings after dependencies were restored from the local cache.
- Updated Release solution build: passed with 14 warnings; historical prototype solution: passed with 20 warnings.
- Updated regression runner: 14 checks passed, including 1,280 generated source–destination cases.
- Local API smoke checks: successful route, unknown artist, and blank input. External-service calls were not exercised. The restricted local environment required `DOTNET_USE_POLLING_FILE_WATCHER=1`.

## Next

1. Rotate/revoke previously exposed service credentials in the provider dashboards.
2. Verify the connected Spotify flow and existing frontend with valid access.
3. Address the remaining external-service warnings and ingestion failure handling.
4. Measure and compare route objectives using fixed evaluation pairs.
