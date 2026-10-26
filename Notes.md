# Notes

## Setup fix
- The app crashed at startup: EF Core 9 detected pending model changes on `AuditContext`.
- Cause: the audit models declared `ClaimId`, `CoverId` and `HttpRequestType` as nullable (`string?`), while the migration created them as NOT NULL. The migrations were also generated with EF Core 6 and edited by hand (`CoverAudit` was missing from the migration's Designer file).
- Fix: made those fields `required` and regenerated a single `InitialCreate` migration with EF Core 9. Safe because Testcontainers creates a fresh database on every run.
- Creating a cover or claim crashed: the MongoDB EF Core provider doesn't support `[BsonDateTimeOptions(DateOnly = true)]`. Removed the attribute from `Claim.Created`, `Cover.StartDate` and `Cover.EndDate`.

## Task 1: Layering
- Moved `ClaimsContext` out of the controller file into `Data/`.
- Added `ClaimsRepository` and `CoversRepository` behind interfaces. Data access moved out of the context and the controllers.
- `GET /Covers/{id}` now queries by id in the database instead of loading all covers into memory.
- Moved the premium calculation out of `CoversController` into `PremiumCalculator` (behind `IPremiumCalculator`) so it can be unit tested. Registered as a singleton since it has no state.
- `Auditer` was created with `new` inside the controllers. Extracted `IAuditer` and injected it through DI, so it can be mocked in tests.
- Added `ClaimsService` and `CoversService`. They hold the business logic (id generation, premium, auditing). Controllers only map service results to HTTP responses.
- Deletes are only audited when something was actually deleted (the template audited every delete request, even for unknown ids).
- Unknown ids now return 404 on GET and DELETE. Create returns 201 with a Location header, delete returns 204.
- Removed unused loggers, and made the compute endpoint synchronous (it was `async` with no awaits, CS1998).

## Task 2: Validation
- Rules live in the services. A failed rule throws `ValidationException`, and a central `IExceptionHandler` turns it into a 400 with a ProblemDetails body, so controllers stay free of try/catch.
- Cover: start date not in the past, end date after start date (added, otherwise the day count goes negative), period max 1 year (`AddYears(1)`, handles leap years).
- Claim: damage cost max 100,000, cover must exist, created date within the cover period (inclusive).
- "Today" comes from an injected `TimeProvider`, so the date rules can be unit tested.

## Task 3: Async auditing
- `Auditer` no longer writes to SQL Server inside the HTTP request. It puts an `AuditMessage` on an in-memory `Channel<T>` (`AuditQueue`), which returns immediately.
- `AuditWorker` (a `BackgroundService`) reads the channel and saves the audits. It creates a DI scope per message, since `AuditContext` is scoped and the worker is a singleton.
- Failures are logged and don't stop the worker.
- Audit timestamps are now UTC, from the injected `TimeProvider` (the template used `DateTime.Now`).
- `IAuditer` didn't change, so services and their tests were untouched.
- Limitation: messages still in the channel are lost if the app crashes or restarts. In production I'd use Azure Service Bus (durable, with retries and a dead-letter queue), with `AuditWorker` becoming the Service Bus consumer.

## Task 4: Unit tests
- Unit tests for both services, with repositories, auditer and premium calculator mocked (NSubstitute). No database needed, they run in milliseconds.
- `FakeTimeProvider` fixes "today", so the start-date rule gives the same result every day.
- Covered: every validation rule including the boundaries (damage cost of exactly 100,000, a period of exactly 1 year, a claim on the first and last day of its cover), nothing saved or audited when validation fails, and delete of an unknown id not audited.
- 17 service tests, plus the 12 premium calculation cases from Task 5.

## Task 5: Premium calculation
- Added `PremiumCalculatorTests` first (12 cases covering each cover type and the 30/180-day boundaries). 11 failed against the original code.
- Bug: the day bands only checked an upper bound and weren't exclusive (`if` instead of `else if`), so each day was charged once per band it fell under, three times for the first 30 days.
- Rewrote the calculation as one multiplier per type (`switch` expression) and one discount per day (`GetDiscount`), matching the spec.
- Assumption: the end date is not counted as a day (a cover from 1 Jan to 31 Jan is 30 days), same as the original code.