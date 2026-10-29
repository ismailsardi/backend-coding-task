# Claims API

Backend for insurance claims handling: create, read and delete covers and claims, with premium calculation, validation and auditing.

## How to run

Requirements: .NET 9 SDK and Docker Desktop (on Apple Silicon, enable Rosetta emulation in Docker for the SQL Server image).

    cd Claims
    dotnet run

Swagger: https://localhost:7052/swagger. SQL Server (audits) and MongoDB (claims and covers) are started automatically with Testcontainers.

Tests, from the repository root:

    dotnet test

## Structure

    Claims/
    ├── Controllers/   HTTP only: call a service, map the result to a status code
    ├── Services/      Business logic: id generation, premium, validation, auditing
    ├── Repositories/  Data access (MongoDB through EF Core)
    ├── Premium/       Premium calculation
    ├── Validation/    ValidationException and the handler that turns it into a 400
    ├── Auditing/      Audit queue, background worker, audit models
    └── Data/          ClaimsContext

Every dependency is injected through an interface, so each layer can be tested on its own.

## Setup fixes

The template did not run as given:
- EF Core 9 detected pending model changes on `AuditContext` at startup. The audit models declared `ClaimId`, `CoverId` and `HttpRequestType` as nullable while the migration created them as NOT NULL, and the EF Core 6 migrations had been edited by hand (`CoverAudit` was missing from the Designer file). I made those fields `required` (an audit row without an id or method makes no sense) and regenerated a single `InitialCreate` migration. Safe here because Testcontainers creates a fresh database on every run.
- Creating a cover or claim crashed: the MongoDB EF Core provider doesn't support `[BsonDateTimeOptions(DateOnly = true)]`. Removed it from `Claim.Created`, `Cover.StartDate` and `Cover.EndDate`.

## Task 1: Layering

- Moved `ClaimsContext` out of the controller file into `Data/`.
- Added repositories, services, `IPremiumCalculator` and `IAuditer`, all registered in DI. Controllers no longer touch the database or create dependencies with `new`.
- `GET /Covers/{id}` now queries by id instead of loading every cover into memory.
- Deletes are only audited when something was actually deleted (the template audited every delete request, even for unknown ids).
- Unknown ids return 404 on GET and DELETE. Create returns 201 with a Location header, delete returns 204.
- Lifetimes: repositories and services are scoped (they depend on a scoped DbContext). The premium calculator, auditer and audit queue are singletons (no per-request state).

## Task 2: Validation

- Rules live in the services. A failed rule throws `ValidationException`, and a central `IExceptionHandler` turns it into a 400 with a ProblemDetails body, so controllers have no try/catch.
- Cover: start date not in the past, end date after start date (added, otherwise the day count goes negative), period of at most 1 year (`AddYears(1)`, handles leap years).
- Claim: damage cost of at most 100,000, the cover must exist, created date within the cover period (inclusive).
- "Today" comes from an injected `TimeProvider`, so the date rules are testable.

## Task 3: Async auditing

- `Auditer` no longer writes to SQL Server inside the HTTP request. It puts an `AuditMessage` on an in-memory `Channel<T>` and returns immediately.
- `AuditWorker` (a `BackgroundService`) reads the channel and saves the audits. It creates a DI scope per message, since `AuditContext` is scoped and the worker is a singleton. Failures are logged and don't stop the worker.
- Audit timestamps are UTC (the template used `DateTime.Now`).
- `IAuditer` kept its signature, so services and their tests were untouched.

## Task 4: Unit tests

- Service tests with repositories, auditer and premium calculator mocked (NSubstitute). No database needed.
- `FakeTimeProvider` fixes "today", so the start-date rule gives the same result every day.
- Covered: every validation rule including the boundaries (damage cost of exactly 100,000, a period of exactly 1 year, a claim on the first and last day of its cover), nothing saved or audited when validation fails, delete of an unknown id not audited, audit messages queued with a UTC timestamp.

## Task 5: Premium calculation

- Wrote `PremiumCalculatorTests` first (12 cases covering each cover type and the 30/180-day boundaries). 11 failed against the original code.
- Bug: the day bands only checked an upper bound and weren't exclusive, so each day was charged once per band it fell under (three times for the first 30 days).
- Rewrote it as one multiplier per type (`switch` expression) and one discount per day (`GetDiscount`), matching the spec.
- Assumption: the end date is not counted as a day (1 Jan to 31 Jan is 30 days), as in the original code.

## With more time

- **Azure Service Bus for auditing.** The in-memory channel loses messages still queued if the app crashes or restarts. Service Bus gives durability, retries and a dead-letter queue, and `AuditWorker` would become its consumer.
- **Request/response DTOs.** The API uses the persistence models directly, so a client can send `Id` or `Premium` (the service overwrites them, but the contract is misleading).
- **Move Testcontainers out of `Program.cs`** into a development-only setup, so the app can run against real connection strings in Azure.
- **Integration tests** for the HTTP layer (status codes, validation responses) with `WebApplicationFactory`.
- **Smaller issues noticed:** `Cover.Type` is stored in Mongo as `"claimType"`; deleting a cover doesn't check for claims that reference it; the `mongo:latest` image isn't pinned; nullable warnings remain on the models.

## How I worked, and how I used AI

I used Claude (Anthropic's assistant) in a chat, as a tutor and reviewer. It had no access to my editor or repository: I pasted code, build output and errors into the chat, and copied suggestions back by hand. VS Code's C# completion also generated one draft (the first version of `ClaimsRepository`), which I rewrote after review.

### Workflow
- Before starting, Claude helped me split the work into small steps: setup, a review of the template, Task 1 in sub-steps (context, repositories, premium calculator, auditer, services, controllers), then Task 5, 2, 4, 3 and the README.
- I committed after each step, and kept a running changelog (`Notes.md`) that became this README.
- For setup problems (pending model changes, the unsupported `DateOnly` attribute, .NET SDK and Docker on Apple Silicon), I pasted the errors and stack traces, and Claude explained the cause. I chose the fixes, for example regenerating the migrations instead of patching the EF Core 6 snapshot.

### Who wrote what
- **Task 1:** I wrote the repositories, the services and the controller changes, following Claude's step-by-step instructions. I pasted each file for review, and fixed the issues it pointed out (wrong repository injected, missing audit calls, the id generated twice, null returned instead of 404).
- **Task 5:** Claude gave me the expected values for the 12 test cases (I checked the logic but skipped the hand calculation) and the structure of the fix (`switch` multiplier, one discount per day band). I ran the tests against the old code first and confirmed 11 failures before rewriting it.
- **Task 2:** Claude provided the exception handler and the validation methods. I wired them into the services and `Program.cs`.
- **Task 4:** Claude wrote both service test classes. I read them through and made sure I understood each mock and assertion before committing.
- **Task 3:** Claude provided the queue, the background worker and the auditer in full, with an explanation of the design (singleton worker creating a scope per message, error handling that keeps the worker alive).
- **README:** drafted by Claude from my notes. I edited it.

### How I validated it
- Built after every change and checked the API manually in Swagger: status codes, the 404s, the 400s for each validation rule, the premium values.
- Test-first for the premium calculation, then the full test suite (`dotnet test`) before each commit.
- What I missed: I never built from a fresh clone. The template's `.gitignore` excludes folders named `Data`, so `Claims/Data/ClaimsContext.cs` was never pushed. It built on my machine and not in the repository. I've fixed the `.gitignore`, and verified that a fresh clone builds and the tests pass.
