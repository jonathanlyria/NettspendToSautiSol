# Project status

Updated: 15 September 2026.

## Completed in the portfolio cleanup

- Documented architecture, environment setup, current limits, and the graph objective.
- Adopted total dissimilarity (`1 - similarity`) as the route cost.
- Fixed disconnected and missing-endpoint route handling; retained isolated artists when loading SQLite.
- Added a regression runner covering graph behaviour and database conversion, including an independent all-pairs comparison.
- Replaced committed credential literals and launch arguments with environment configuration, and removed credential/token logging in the main applications.
- Removed generated build output and restored dependency folders from version control; retained source, the included database, and the original report.
- Added a build/check workflow. Hosted run results are available in the repository’s Actions tab.

## Spotify repair — 15 September 2026

- Fixed the reproduced macOS `CookieContainer` initialisation failure by disabling unused cookie handling in the backend API client. HTTPS certificate validation remains enabled.
- Migrated playlist creation and item addition to current Spotify development-mode endpoints; replaced playlist song selection's removed artist top-tracks call with artist-filtered search.
- Kept authentication headers per request; removed browser logging of authorisation-code request bodies.
- Added safe stage-specific errors, empty-selection protection and ordered batches of 100 songs. Partial failures identify the created playlist instead of silently retrying writes.
- Added 9 credential-free Spotify regression checks and included them in CI.
- Live read-only check: catalogue authentication succeeded; Nettspend → Cleo Sol selected 27 songs across 11 artists. The fresh browser sign-in and playlist creation succeeded; Spotify displayed 27 songs, about 1 hour 15 minutes, in the new playlist.
- Backend restarted with the saved local configuration; frontend remains on port 8080. The blank `.env.example` template is restored; `.env` is unchanged.

## Verification

- Original solution compiled locally with 22 warnings after dependencies were restored from the local cache.
- Updated Release solution build: passed with 14 warnings; historical prototype solution: passed with 20 warnings.
- Updated regression runner: 14 checks passed, including 1,280 generated source–destination cases.
- Local API smoke checks: successful route, unknown artist, and blank input. These original smoke checks did not exercise external services; the live read-only Spotify check is recorded above. The restricted local environment required `DOTNET_USE_POLLING_FILE_WATCHER=1`.

## Next

1. Rotate/revoke previously exposed service credentials in the provider dashboards.
2. Verify the hosted CI run and keep its status current after publication.
3. Migrate the separate network expander from removed Spotify endpoints/fields, and address remaining warnings and ingestion failure handling.
4. Measure and compare route objectives using fixed evaluation pairs.
