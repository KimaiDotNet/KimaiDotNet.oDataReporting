# Feature Spec: Upgrade OData Service to .NET 10 LTS

**Status**: Draft  
**Author**: Mark  
**Created**: 2026-05-26  
**Last updated**: 2026-05-26  
**GitHub Issue**: [#15](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/15)  
**Epic**: [#4 — .NET 10 LTS Upgrade](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/4)  
**Related ADRs**: None

---

## Summary

`KimaiDotNet.Reporting.ODataService` currently targets .NET 7, which reached end-of-support in May 2024. This feature upgrades the project to .NET 10 LTS — the current long-term support release — restoring access to security patches, modern runtime APIs, and continued OS support. All eight existing OData controllers and the chaos engineering subsystem (Polly + Simmy) must remain fully functional after the upgrade. The primary risk is compatibility of `Polly.Contrib.Simmy` 0.3.0, which was built against Polly v6/v7; Polly v8 (the .NET 10–era release) is a ground-up rewrite that absorbed chaos strategies into the core library, and a direct package substitution may be required.

## Goals

- Upgrade `TargetFramework` from `net7.0` to `net10.0` in `KimaiDotNet.Reporting.ODataService.csproj`.
- Update all NuGet package references to versions that support `net10.0`, `net8.0`, or `netstandard2.1`/`netstandard2.0`.
- Restore or preserve chaos engineering functionality (fault injection, latency injection) using a .NET 10–compatible approach.
- Update the Docker base image to an official .NET 10 runtime image.
- Confirm all eight OData controllers (`Activity`, `Customer`, `Export`, `Project`, `Team`, `TeamMembership`, `Timesheet`, `User`) continue to respond correctly.
- Validate compatibility with .NET 11 Preview 4 is possible (tracked in envisioning; smoke-test only in this issue).

## Non-goals

- Adding new OData entity sets, endpoints, or EDM model changes.
- Upgrading to .NET 11 as the primary target framework (secondary validation only).
- Refactoring chaos engineering architecture beyond the minimum required to restore compatibility.
- Replacing `System.Data.SqlClient` with `Microsoft.Data.SqlClient` (deprecated package; tracked separately).
- Changing the OData read-only contract or any configuration schema key names.
- Updating the SiSense, Power BI, or Tableau dashboard files.

## Requirements

### Functional

1. The system shall build and run on .NET 10 with `TargetFramework` set to `net10.0`.
2. The system shall serve all existing OData endpoints with identical HTTP verb support, query option support, and response shapes as before the upgrade.
3. The system shall honour all `GeneralChaosOptions` and `OperationChaosOptions` configuration settings: when chaos is enabled, fault injection and latency injection shall behave as they did on .NET 7.
4. The system shall read configuration from `appsettings.json`, `appsettings.Development.json`, and environment variables without any change to existing configuration key names or value types.
5. The system shall produce a Docker image using a .NET 10 base image that starts successfully and passes a basic OData metadata request (`GET /$metadata`).
6. The system shall compile without errors or `.NET platform compatibility` suppressions (`<NoWarn>` entries) introduced solely to mask upgrade-related warnings.

### Non-functional

1. The service shall start in under 10 seconds on equivalent hardware to the pre-upgrade environment.
2. All existing xUnit tests shall pass on .NET 10 without modification to test assertions or expected values.
3. No package reference shall target a version with a known CVE at the time of the upgrade.
4. The Docker image build shall complete using a standard multi-stage `dotnet publish` pattern with no custom workarounds.

## Package compatibility matrix

The following packages are at risk and must be explicitly resolved before the feature is considered complete.

| Package | Current version | Risk | Required action |
|---------|----------------|------|-----------------|
| `Polly.Contrib.Simmy` | 0.3.0 | **High** — targets Polly v6/v7; Polly v8 is a breaking rewrite | Investigate: replace with Polly v8 built-in chaos strategies or confirm a maintained .NET 10–compatible release exists |
| `Microsoft.Extensions.Http.Polly` | 7.0.3 | **High** — .NET 7 era; must be updated for .NET 10 | Update to .NET 10–compatible version |
| `Microsoft.AspNetCore.OData` | 8.0.12 | **Medium** — v9.x exists; check .NET 10 support in v8 vs v9 | Upgrade to latest stable v9.x if v8 does not support net10.0 |
| `MarkZither.KimaiDotNet.ApiClient` | 0.4.0-beta0001 | **Medium** — external beta NuGet; .NET 10 compatibility unknown | Confirm compatibility; build from source if no compatible release exists |
| `MonkeyCache.LiteDB` / `MonkeyCache.FileStore` | 2.0.1 | **Medium** — verify active maintenance and .NET 10 TFM support | Confirm compatibility; evaluate alternatives if unmaintained |
| `MiniProfiler.AspNetCore.Mvc` | 4.2.22 | **Low–Medium** — check if 4.3.x+ is required for net10.0 | Update to latest stable 4.x or 5.x |
| `Microsoft.OpenApi.OData` | 1.2.0 | **Low** — typically netstandard2.0; confirm | Verify TFM support |
| `System.Data.SqlClient` | 4.8.5 | **Low** (for this feature) — deprecated but likely still resolves | Defer replacement to separate issue; confirm it resolves on net10.0 |
| `Swashbuckle.AspNetCore` | 6.5.0 | **Low** — 7.x may be needed for net10.0 | Update to latest stable |
| `CsvHelper` | 30.0.1 | **Low** — typically netstandard2.0 | Confirm |

## Acceptance criteria

- [ ] `<TargetFramework>net10.0</TargetFramework>` is present in `KimaiDotNet.Reporting.ODataService.csproj`.
- [ ] `dotnet build` completes with zero errors and zero framework-compatibility warnings.
- [ ] All package references resolve to versions that declare `net10.0`, `net8.0`, `netstandard2.1`, or `netstandard2.0` as a supported target (verified via `dotnet list package`).
- [ ] `Polly.Contrib.Simmy` is either confirmed .NET 10–compatible, replaced with an equivalent chaos mechanism, or replaced with a stub and a tracking issue is created — the outcome is documented in the open questions table below.
- [ ] Chaos engineering is functional end-to-end: with `GeneralChaosOptions:Enabled = true`, injected faults are observable in a local integration test or manual run.
- [ ] All eight OData controller endpoints (`/Activities`, `/Customers`, `/Exports`, `/Projects`, `/Teams`, `/TeamMemberships`, `/Timesheets`, `/Users`) return HTTP 200 for valid authenticated requests in a local run.
- [ ] `GET /$metadata` returns a valid EDMX document.
- [ ] `docker build` succeeds from the repository root and the resulting image passes a `GET /$metadata` smoke test.
- [ ] `dotnet test` passes with no test failures on net10.0.
- [ ] `appsettings.json` and `appsettings.Development.json` require no schema changes.

## Open questions

| # | Question | Owner | Status |
|---|----------|-------|--------|
| 1 | Is `Polly.Contrib.Simmy` 0.3.0 compatible with .NET 10? If not, does a maintained replacement exist (e.g., Polly v8 built-in chaos strategies via `Polly.Extensions`)? This is the primary `needs-investigation` flag on issue #15. | Mark | Open |
| 2 | Does `MarkZither.KimaiDotNet.ApiClient` 0.4.0-beta0001 have a .NET 10–compatible release on NuGet? If not, should it be built from source or pinned via a local package source? | Mark | Open |
| 3 | Do `MonkeyCache.LiteDB` and `MonkeyCache.FileStore` 2.0.1 support .NET 10? Are these packages still actively maintained? If not, what is the preferred caching alternative? | Mark | Open |
| 4 | Should `System.Data.SqlClient` (deprecated) be replaced with `Microsoft.Data.SqlClient` as part of this upgrade, or is that tracked as a separate issue? | Mark | Open |
| 5 | Does `Microsoft.AspNetCore.OData` v8.x support `net10.0`, or is an upgrade to v9.x required? If v9.x introduces breaking changes to existing controller code, those changes should be scoped explicitly. | Mark | Open |

## Dependencies

- **GitHub Issue #4** — .NET 10 LTS Upgrade (parent epic); this issue is a child of that epic.
- **`MarkZither.KimaiDotNet.ApiClient`** — must be available at a .NET 10–compatible version before this feature can be closed; see Open Question 2.
- **Polly/Simmy compatibility resolution** — Open Question 1 must be answered before acceptance criterion for chaos engineering can be verified.
- **.NET 10 SDK** — must be installed in the local development environment and in CI.
