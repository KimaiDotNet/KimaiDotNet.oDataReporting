# Tasks: Upgrade OData Service to .NET 10 LTS

**Feature**: Upgrade OData Service to .NET 10 LTS
**Issue**: [#15](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/15)
**Epic**: [#4](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/4)
**ADR**: [ADR-0001](../architecture/decisions/ADR-0001-migrate-polly-chaos-to-v8-polly-simmy.md)
**Date**: 2026-05-26

---

## Phase 0 — Baseline Lock

- [x] Create `global.json` pinning SDK to `10.0.300` (`rollForward: latestPatch`); confirm `net7.0` build is clean under SDK 10.0.300; record `dotnet list package` output
  - _Note: .NET 7 SDK is not installed; SDK 10.0.300 builds net7.0 TFM natively_
  - _Result: build succeeded with 29 warnings (0 errors); baseline recorded in `docs/features/upgrade-to-net10/baseline-packages.txt`_

## Phase 1 — Framework Version Bump

- [x] Bump `<TargetFramework>` to `net10.0` and update Dockerfile base images in `src/KimaiDotNet.Reporting.ODataService/`
  - _Result: restore succeeded (5 warnings, 0 errors); build succeeded targeting net10.0 (31 warnings, 0 errors). All packages resolved — no NU1701 compat warnings. Pre-expected failures (Polly.Contrib.Simmy, Microsoft.Extensions.Http.Polly) did NOT occur._

## Phase 2 — Low-Risk Package Updates

- [ ] [P] Investigate `MarkZither.KimaiDotNet.ApiClient` 0.4.0-beta0001 .NET 10 compatibility (OQ2)
- [ ] [P] Investigate `MonkeyCache.LiteDB` / `MonkeyCache.FileStore` 2.0.1 .NET 10 compatibility and caching replacement plan (OQ3)
- [ ] Update low-risk NuGet packages for net10.0 compatibility in `KimaiDotNet.Reporting.ODataService.csproj`

## Phase 3 — OData Package Upgrade

- [ ] Upgrade `Microsoft.AspNetCore.OData` to v9.x and resolve controller breaking changes in `src/KimaiDotNet.Reporting.ODataService/Controllers/`

## Phase 4 — Polly v8 Migration (ADR-0001)

- [ ] [P] Migrate `Extensions/PollyContextExtensions.cs` to Polly v8 `ResilienceContext` API
- [ ] [P] Migrate `Extensions/SimmyContextExtensions.cs` to Polly v8 `ResiliencePropertyKey<T>` API
- [ ] Rewrite `Extensions/DependencyInjectionExtensions.cs` to embed chaos strategies in Polly v8 pipeline
- [ ] Rewrite Polly resilience pipeline registration in `Program.cs` using `AddResiliencePipeline` and `AddResilienceHandler`

## Phase 5 — Remaining Compatibility Verification

- [ ] Verify package security and resolve any CVEs in `KimaiDotNet.Reporting.ODataService.csproj`

## Phase 6 — Docker Image Update

- [ ] Verify net10.0 multi-stage Docker build and run `GET /$metadata` smoke test for `Dockerfile`

## Phase 7 — Test Strategy and Validation

- [ ] Create xUnit test project `tests/KimaiDotNet.Reporting.ODataService.Tests/` with OData endpoint integration tests and chaos unit tests targeting net10.0

---

## Task Detail

### T01 — Record .NET 7 clean build baseline

**Phase**: 0 — Baseline Lock
**Size**: S
**Dependencies**: None
**Parallelizable**: No

Run `dotnet build`, `dotnet test` (or note no test project exists), and `dotnet list package`
on the current .NET 7 codebase to establish a known-good baseline before any changes.
Record the output for comparison after the upgrade. No file changes are made in this task;
results are captured as a comment on issue #15.

**Acceptance Criteria**:
- `dotnet build` output recorded showing zero errors on .NET 7
- `dotnet list package` output saved as a comment on issue #15

---

### T02 — Bump TargetFramework to net10.0 and update Dockerfile

**Phase**: 1 — Framework Version Bump
**Size**: S
**Dependencies**: T01
**Parallelizable**: No

Change `<TargetFramework>net7.0</TargetFramework>` → `net10.0` in
`KimaiDotNet.Reporting.ODataService.csproj`. Update both `FROM` lines in `Dockerfile`
from `aspnet:7.0` / `sdk:7.0` → `aspnet:10.0` / `sdk:10.0`. Run `dotnet restore` and
record restore failures — these are expected at this phase and resolved in later phases.

**Acceptance Criteria**:
- `<TargetFramework>net10.0</TargetFramework>` is present in the csproj
- Both Dockerfile `FROM` lines reference .NET 10 base images
- Commit: `chore: bump TargetFramework to net10.0 and update Dockerfile base images`

---

### T03 — Investigate MarkZither.KimaiDotNet.ApiClient .NET 10 compatibility (OQ2)

**Phase**: 2 — Low-Risk Package Updates
**Size**: S
**Dependencies**: T02
**Parallelizable**: Yes (with T04)

Run `dotnet restore` after the TFM change to check if `MarkZither.KimaiDotNet.ApiClient`
0.4.0-beta0001 resolves on net10.0. If restore fails, check NuGet for a newer compatible
release. If none exists, document a plan to reference the package from source via a
`<ProjectReference>` or local NuGet source.

**Acceptance Criteria**:
- Documented outcome: package resolves with no action, a compatible version exists (version
  recorded), or a source-build plan is documented
- Resolution captured as a comment on this issue

---

### T04 — Investigate MonkeyCache .NET 10 compatibility and caching replacement plan (OQ3)

**Phase**: 2 — Low-Risk Package Updates
**Size**: S
**Dependencies**: T02
**Parallelizable**: Yes (with T03)

Check NuGet for a version ≥ 2.0.1 of `MonkeyCache.LiteDB` / `MonkeyCache.FileStore` that
declares net10.0, net8.0, or netstandard2.1 support. If the package is unmaintained (last
release >2 years, no net8+ TFM), document a replacement plan using
`Microsoft.Extensions.Caching.Memory` with a thin wrapper preserving `Barrel.Get<T>` /
`Barrel.Add<T>` semantics in `Program.cs`.

**Acceptance Criteria**:
- Documented outcome: package resolves, update available, or replacement plan documented
- Resolution captured as a comment on this issue

---

### T05 — Update low-risk NuGet packages for net10.0 compatibility

**Phase**: 2 — Low-Risk Package Updates
**Size**: M
**Dependencies**: T03, T04
**Parallelizable**: No

Update the following packages (one `dotnet add package` call each, verifying restore succeeds
after each): `Microsoft.Extensions.Http` (latest), `CsvHelper` (latest stable),
`Microsoft.OpenApi.OData` (latest 1.x), `MiniProfiler.AspNetCore.Mvc` (4.3.x or 5.x),
`Swashbuckle.AspNetCore` (7.x), `Microsoft.VisualStudio.Azure.Containers.Tools.Targets`
(latest). Apply the resolutions for OQ2 and OQ3 from T03 and T04.

**Acceptance Criteria**:
- `dotnet restore` succeeds after all updates
- Each updated package resolves to a version supporting net10.0, net8.0, netstandard2.1, or netstandard2.0
- Commit: `chore: update low-risk packages for net10.0 compatibility`

---

### T06 — Upgrade Microsoft.AspNetCore.OData to v9.x

**Phase**: 3 — OData Package Upgrade
**Size**: M
**Dependencies**: T05
**Parallelizable**: No

Update `Microsoft.AspNetCore.OData` from 8.0.12 to the latest stable v9.x release. Review
the OData v9 migration guide for controller-level breaking changes (namespace adjustments,
`EnableQueryAttribute` parameter changes, EDM model registration in `AddOData`). Resolve any
compilation errors in all eight controllers and verify `GET /$metadata` responds with a valid
EDMX via a local `dotnet run` smoke test.

**Acceptance Criteria**:
- `Microsoft.AspNetCore.OData` csproj version is 9.x
- `dotnet build` succeeds with zero errors across all eight controllers
- `GET /$metadata` returns valid EDMX in a local `dotnet run` smoke test
- Commit: `feat: upgrade Microsoft.AspNetCore.OData to v9.x for net10.0`

---

### T07 — Migrate PollyContextExtensions.cs to Polly v8 ResilienceContext API

**Phase**: 4 — Polly v8 Migration
**Size**: S
**Dependencies**: T05
**Parallelizable**: Yes (with T08)

Replace the `Polly.Context` parameter type with `ResilienceContext` throughout
`Extensions/PollyContextExtensions.cs`. Update `WithLogger<T>` to use
`context.Properties.Set(new ResiliencePropertyKey<ILogger>("ILogger"), logger)` and
`GetLogger` to use `context.Properties.TryGetValue(...)`. Preserve public method signatures
so callers are unaffected.

**Acceptance Criteria**:
- `PollyContextExtensions.cs` uses `ResilienceContext` instead of `Polly.Context`
- `WithLogger<T>` and `GetLogger` public signatures are unchanged
- `dotnet build` succeeds after this change

---

### T08 — Migrate SimmyContextExtensions.cs to Polly v8 ResiliencePropertyKey<T> API

**Phase**: 4 — Polly v8 Migration
**Size**: S
**Dependencies**: T05
**Parallelizable**: Yes (with T07)

Replace `Polly.Context` with `ResilienceContext` throughout `Extensions/SimmyContextExtensions.cs`.
Define `ChaosSettingsKey` as `static readonly ResiliencePropertyKey<GeneralChaosOptions>`.
Update `WithChaosSettings` to use `context.Properties.Set(ChaosSettingsKey, options)` and
`GetChaosSettings` / `GetOperationChaosSettings` to use `context.Properties.TryGetValue(...)`.
Preserve all public method names and return types.

**Acceptance Criteria**:
- `SimmyContextExtensions.cs` uses `ResilienceContext` and `ResiliencePropertyKey<GeneralChaosOptions>`
- `ChaosSettingsKey` is `static readonly ResiliencePropertyKey<GeneralChaosOptions>`
- `GetChaosSettings` and `GetOperationChaosSettings` return types and semantics are unchanged
- `dotnet build` succeeds after this change

---

### T09 — Rewrite DependencyInjectionExtensions.cs for Polly v8 chaos pipeline

**Phase**: 4 — Polly v8 Migration
**Size**: M
**Dependencies**: T07, T08
**Parallelizable**: No

Remove `AddHttpChaosInjectors(this IPolicyRegistry<string> registry)` and replace with a new
extension method on `IHttpResiliencePipelineBuilder` that adds `AddChaosException` and
`AddChaosLatency` strategies using generator delegates reading from `ResilienceContext`.
Update private helper methods to accept `ResilienceContext` via typed `args.Context`.
Remove `using Polly.Registry` and `using Polly.Contrib.Simmy` imports. Preserve
`CreateSqlException()` and other non-Polly helpers.

**Acceptance Criteria**:
- `AddHttpChaosInjectors` is removed; chaos strategies are embedded during pipeline registration
- `AddChaosException` and `AddChaosLatency` are present in the new extension method
- No `using Polly.Contrib.Simmy` or `using Polly.Registry` references remain
- `dotnet build` succeeds after this change

---

### T10 — Rewrite Polly resilience pipeline registration in Program.cs

**Phase**: 4 — Polly v8 Migration
**Size**: M
**Dependencies**: T09
**Parallelizable**: No

Replace `builder.Services.AddPolicyRegistry()` and `AddHttpChaosInjectors` calls with
`builder.Services.AddResiliencePipeline<string, HttpResponseMessage>` embedding retry and
chaos strategies. Replace `.AddPolicyHandlerFromRegistry("WrappedChoas")` with
`.AddResilienceHandler(...)` from `Microsoft.Extensions.Http.Resilience`. Remove all
`Polly.Contrib.Simmy` using directives. Add `Polly.Core` ≥ 8.3.0 and
`Microsoft.Extensions.Http.Resilience` to the csproj; remove `Polly.Contrib.Simmy` and
`Microsoft.Extensions.Http.Polly`.

**Acceptance Criteria**:
- `Program.cs` uses `AddResiliencePipeline` and `AddResilienceHandler` from Polly v8
- No `Polly.Contrib.Simmy`, `Microsoft.Extensions.Http.Polly`, or Polly v7 references remain
- `dotnet build` succeeds with zero errors
- `dotnet run` with `GeneralChaosOptions:Enabled = false` returns HTTP 200 on all eight OData endpoints
- Commit: `feat: migrate chaos engineering to Polly.Core 8.3.0+ with built-in chaos strategies (ADR-0001)`

---

### T11 — Verify package security and resolve CVEs

**Phase**: 5 — Remaining Compatibility Verification
**Size**: S
**Dependencies**: T06, T10
**Parallelizable**: No

Run `dotnet list package --vulnerable` and `dotnet list package --outdated`. Address any
high or critical CVEs before the issue can be closed. Confirm `System.Data.SqlClient` 4.8.5
resolves on net10.0 via compatibility mode without replacement (deferred per spec non-goal).

**Acceptance Criteria**:
- `dotnet list package --vulnerable` returns no high or critical CVEs
- `System.Data.SqlClient` 4.8.5 resolves on net10.0 (outcome documented)
- Any security-driven updates are committed: `chore: resolve remaining package compatibility for net10.0`

---

### T12 — Verify net10.0 Docker multi-stage build and run metadata smoke test

**Phase**: 6 — Docker Image Update
**Size**: S
**Dependencies**: T11
**Parallelizable**: No

Run `docker build` from the repository root using the updated multi-stage `Dockerfile`.
Start the resulting image and issue `GET /$metadata` to confirm it returns a valid EDMX
document. Confirm no `.NET platform compatibility` warnings exist and no `<NoWarn>` entries
were added solely to mask upgrade-related warnings.

**Acceptance Criteria**:
- `docker build` completes with no errors
- `GET /$metadata` against the running container returns HTTP 200 with valid EDMX
- No `<NoWarn>` entries introduced solely to suppress compatibility warnings
- Commit: `chore: verify net10.0 Docker build and metadata smoke test`

---

### T13 — Create xUnit test project with OData integration tests and chaos unit tests

**Phase**: 7 — Test Strategy and Validation
**Size**: L
**Dependencies**: T12
**Parallelizable**: No

Create a new xUnit test project at `tests/KimaiDotNet.Reporting.ODataService.Tests/`
targeting net10.0 and add it to the solution. Implement integration tests using
`Microsoft.AspNetCore.Mvc.Testing` for all eight OData endpoints (HTTP 200) and
`GET /$metadata` (valid EDMX). Implement unit tests: `AddChaosException` throws the
configured exception type at `InjectionRate = 1.0`, `AddChaosLatency` delays responses by
at least the configured `LatencyMs`. Add a configuration binding test for
`GeneralChaosOptions` and `OperationChaosOptions`.

**Acceptance Criteria**:
- Test project exists at `tests/KimaiDotNet.Reporting.ODataService.Tests/` targeting net10.0
- `dotnet test` passes with zero failures
- Integration tests cover all eight OData endpoints and `GET /$metadata`
- Unit tests cover chaos exception injection and chaos latency injection
- Configuration binding tests pass for `GeneralChaosOptions` and `OperationChaosOptions`
