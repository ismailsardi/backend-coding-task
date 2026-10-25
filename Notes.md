# Notes

## Setup fix
- The app crashed at startup: EF Core 9 detected pending model changes on `AuditContext`.
- Cause: the audit models declared `ClaimId`, `CoverId` and `HttpRequestType` as nullable (`string?`), while the migration created them as NOT NULL. The migrations were also generated with EF Core 6 and edited by hand (`CoverAudit` was missing from the migration's Designer file).
- Fix: made those fields `required` and regenerated a single `InitialCreate` migration with EF Core 9. Safe because Testcontainers creates a fresh database on every run.