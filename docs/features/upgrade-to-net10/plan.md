# Plan: Upgrade OData Service to .NET 10 LTS

**Feature**: Upgrade OData Service to .NET 10 LTS  
**Issue**: [#15](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/15)  
**Epic**: [#4 — .NET 10 LTS Upgrade](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/4)  
**ADRs**: [ADR-0001 — Migrate Polly Chaos to v8 (Polly.Core ≥ 8.3.0)](../architecture/decisions/ADR-0001-migrate-polly-chaos-to-v8-polly-simmy.md)  
**Date**: 2026-05-26

---

## Dependency upgrade sequence

The upgrade is executed in seven sequential phases. Each phase must leave the project in a
buildable state before the next phase begins. Commit after each phase.

### Phase 0 — Baseline lock

**Objective**: establish a known-good baseline before any changes.

**Context**: the .NET 7 SDK is not installed on the build machine. The .NET 10 SDK (10.0.300)
can build projects that target `net7.0` because the SDK carries multi-targeting support for all
previous in-support and out-of-support TFMs. A `global.json` file is created at the repository
root to pin the SDK to `10.0.300`, ensuring reproducible builds across machines regardless of
which other SDK versions are installed.

1. Create `global.json` at the repository root:
   ```json
   {
     "sdk": {
       "version": "10.0.300",
       "rollForward": "latestPatch"
     }
   }
   ```
2. Run `dotnet build` and confirm the current `net7.0` build is clean under SDK 10.0.300.
3. Run `dotnet test` (or note that no test project exists yet — see Risk R4).
4. Record the current `dotnet list package` output for comparison after the upgrade.

Commit: `chore: pin SDK to 10.0.300 via global.json and record net7 baseline before net10 upgrade`.

---

### Phase 1 — Framework version bump

**Files changed**: `KimaiDotNet.Reporting.ODataService.csproj`, `Dockerfile`

**Steps**:

1. Change `<TargetFramework>net7.0</TargetFramework>` → `<TargetFramework>net10.0</TargetFramework>`.
2. Update `Dockerfile`:
   - `FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base` → `mcr.microsoft.com/dotnet/aspnet:10.0`
   - `FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build` → `mcr.microsoft.com/dotnet/sdk:10.0`
3. Run `dotnet restore`. Expect restore failures — many packages will not resolve yet.
   These failures are expected at this phase; the remaining phases address them in order.
4. Commit: `chore: bump TargetFramework to net10.0 and update Dockerfile base images`.

**Expected errors after this phase** (resolved in later phases):
- `Polly.Contrib.Simmy` / `Microsoft.Extensions.Http.Polly` version conflicts
- `Microsoft.AspNetCore.OData` may not resolve for net10.0 on v8.x
- Possible `MarkZither.KimaiDotNet.ApiClient` restore failure

---

### Phase 2 — Low-risk package updates

**Objective**: update packages that target `netstandard2.0` or have confirmed net10.0 support,
in isolation before the high-risk changes.

Update in this order (one `dotnet add package` call each, then verify restore succeeds):

1. `Microsoft.Extensions.Http` → latest stable (framework-provided version or latest 10.x)
2. `CsvHelper` → latest stable (currently netstandard2.0; verify TFM in NuGet)
3. `Microsoft.OpenApi.OData` → latest stable 1.x (netstandard2.0 target)
4. `MiniProfiler.AspNetCore.Mvc` → latest stable 4.3.x or 5.x
5. `Swashbuckle.AspNetCore` → 7.x latest stable (confirmed .NET 10 support)
6. `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` → latest stable (dev-time only)

**Verify `MarkZither.KimaiDotNet.ApiClient`** (OQ2):

- Run `dotnet restore` after TFM change; if the package resolves, no action is needed.
- If restore fails: check NuGet for a newer release at
  `https://www.nuget.org/packages/MarkZither.KimaiDotNet.ApiClient`.
- If no compatible release exists: clone the source repository, build targeting `net10.0` or
  `netstandard2.1`, and reference the output via a local `<PackageReference>` with a local
  NuGet source, or add the project directly to the solution with a `<ProjectReference>`.

**Verify `MonkeyCache.LiteDB` / `MonkeyCache.FileStore`** (OQ3):

- Check NuGet for a version ≥ 2.0.1 that declares net10.0, net8.0, or netstandard2.1.
- If an updated version exists: upgrade.
- If the package is unmaintained (last release > 2 years ago, no net8+ TFM): replace with
  `Microsoft.Extensions.Caching.Memory` for in-process caching and
  `Microsoft.Extensions.Caching.Distributed` for distributed caching. The replacement must
  preserve the `Barrel.ApplicationId` / `Barrel.EncryptionKey` semantics used in `Program.cs`
  — create a thin wrapper service that provides equivalent `Get<T>` / `Add<T>` operations.

Commit: `chore: update low-risk packages for net10.0 compatibility`.

---

### Phase 3 — OData package upgrade

**Files changed**: `KimaiDotNet.Reporting.ODataService.csproj`, potentially all eight
controller files if OData v9 introduces breaking API changes.

**Steps**:

1. Update `Microsoft.AspNetCore.OData` from 8.0.12 to the latest stable 9.x release.
2. Run `dotnet build`. Resolve any OData v9 breaking changes:
   - Review the [OData v9 migration guide](https://github.com/OData/AspNetCoreOData/blob/main/docs/migration-guide.md)
     for controller-level breaking changes.
   - Common v8→v9 changes: namespace adjustments, `EnableQueryAttribute` parameter changes,
     EDM model registration changes in `AddOData(opt => ...)`.
3. Verify all eight controllers (`Activity`, `Customer`, `Export`, `Project`, `Team`,
   `TeamMembership`, `Timesheet`, `User`) compile without errors.
4. Verify `$metadata` responds correctly with a local `dotnet run` smoke test.

Commit: `feat: upgrade Microsoft.AspNetCore.OData to v9.x for net10.0`.

---

### Phase 4 — Polly v8 (Polly.Core ≥ 8.3.0) migration (primary risk — ADR-0001)

**Objective**: replace `Polly.Contrib.Simmy` 0.3.0 and `Microsoft.Extensions.Http.Polly` 7.0.3
with `Polly.Core` ≥ 8.3.0 and `Microsoft.Extensions.Http.Resilience`. Chaos strategies
(`AddChaosLatency`, `AddChaosFault`, `AddChaosOutcome`, `AddChaosBehavior`) are built into
`Polly.Core` ≥ 8.3.0 — no separate `Polly.Simmy` package reference is required. Preserve all
`GeneralChaosOptions` and `OperationChaosOptions` configuration semantics.

**Package changes**:

```xml
<!-- Remove -->
<PackageReference Include="Polly.Contrib.Simmy" Version="0.3.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="7.0.3" />

<!-- Add -->
<PackageReference Include="Polly.Core" Version="8.3.0" />
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="[latest net10.0-compatible]" />
```

`Polly.Simmy` is included transitively via `Polly.Core` ≥ 8.3.0. `Polly.Core` is pinned
explicitly to ensure the minimum version with built-in chaos strategies is enforced.

#### 4.1 — Rewrite `Extensions/PollyContextExtensions.cs`

- Replace `Polly.Context` parameter type with `ResilienceContext`
  (`Polly.ResilienceContext`).
- Replace `context[LoggerKey] = logger` with
  `context.Properties.Set(new ResiliencePropertyKey<ILogger>("ILogger"), logger)`.
- Replace `context.TryGetValue(LoggerKey, out object logger)` with the typed
  `context.Properties.TryGetValue(...)` equivalent.
- The public method signatures (`WithLogger<T>`, `GetLogger`) are preserved.

#### 4.2 — Rewrite `Extensions/SimmyContextExtensions.cs`

- Replace `Polly.Context` with `ResilienceContext` throughout.
- Define `ChaosSettingsKey` as `static readonly ResiliencePropertyKey<GeneralChaosOptions>`.
- Replace `context[ChaosSettings] = options` with `context.Properties.Set(ChaosSettingsKey, options)`.
- Replace `context.TryGetValue(ChaosSettings, out object setting)` with
  `context.Properties.TryGetValue(ChaosSettingsKey, out var setting)`.
- `GetChaosSettings`, `GetOperationChaosSettings` return types and behaviour are preserved.
- `context.OperationKey` (used via `context.OperationKey` in the helper) is available on
  `ResilienceContext.OperationKey` — no change required here.

#### 4.3 — Rewrite `Extensions/DependencyInjectionExtensions.cs`

- Remove `AddHttpChaosInjectors(this IPolicyRegistry<string> registry)` — this method wraps
  existing policies post-registration, a pattern that does not exist in Polly v8.
- Replace with a new extension method (or move logic into `Program.cs` inline) that accepts
  an `IHttpResiliencePipelineBuilder` and adds chaos strategies:

  ```csharp
  // Conceptual structure — not final code
  public static IHttpResiliencePipelineBuilder AddOperationChaosStrategies(
      this IHttpResiliencePipelineBuilder builder,
      IServiceProvider sp)
  {
      return builder
          .AddChaosException(new ChaosExceptionStrategyOptions<HttpResponseMessage>
          {
              EnabledGenerator    = args => GetEnabled(args.Context),
              InjectionRateGenerator = args => GetInjectionRate(args.Context),
              ExceptionGenerator  = args => GetException(args.Context)
          })
          .AddChaosLatency(new ChaosLatencyStrategyOptions<HttpResponseMessage>
          {
              EnabledGenerator    = args => GetEnabled(args.Context),
              InjectionRateGenerator = args => GetInjectionRate(args.Context),
              LatencyGenerator    = args => GetLatency(args.Context)
          });
  }
  ```

- Private helper methods (`GetEnabled`, `GetInjectionRate`, `GetException`, `GetLatency`) are
  updated to accept `ResilienceContext` via the typed `args.Context`.
- `CreateSqlException()` and all non-Polly helpers are preserved as-is.
- Remove `using Polly.Registry`; chaos types are available via the `Polly` namespace from `Polly.Core` ≥ 8.3.0.

#### 4.4 — Rewrite Polly registration in `Program.cs`

Replace the v7 policy registration block with Polly v8 equivalents:

```csharp
// Conceptual structure — not final code
builder.Services.AddResiliencePipeline<string, HttpResponseMessage>(
    "KimaiResilience",
    (pipelineBuilder, context) =>
    {
        pipelineBuilder
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(10),
                OnRetry = static args =>
                {
                    args.Context.GetLogger()?.LogError(...);
                    return default;
                }
            })
            .AddOperationChaosStrategies(context.ServiceProvider);
    });

builder.Services.AddHttpClient(Constants.HttpClients.Kimai, httpClient =>
{
    httpClient.BaseAddress = new Uri(kimaiOptions.Url);
    httpClient.DefaultRequestHeaders.Add("X-AUTH-USER", kimaiOptions.Username);
    httpClient.DefaultRequestHeaders.Add("X-AUTH-TOKEN", kimaiOptions.Password);
})
.AddResilienceHandler("KimaiResilience", /* pipeline from above */);
```

- Remove `builder.Services.AddPolicyRegistry()` and `policyRegistry.AddHttpChaosInjectors()`.
- Remove `using Polly.Contrib.Simmy` and `using Polly.Contrib.Simmy.Latency` imports.

**Verification after Phase 4**:

1. `dotnet build` — zero errors.
2. `dotnet run` with `GeneralChaosOptions:Enabled = false` in `appsettings.Development.json`
   — all eight OData endpoints respond HTTP 200.
3. Enable chaos in `appsettings.Development.json` for one operation and confirm latency
   injection is observable in response times (manual test or integration test — see Phase 7).

Commit: `feat: migrate chaos engineering to Polly.Core 8.3.0+ with built-in chaos strategies (ADR-0001)`.

---

### Phase 5 — Remaining compatibility verification

**Steps** (complete after OQ2 and OQ3 are resolved):

1. Run `dotnet list package --vulnerable` — address any CVEs before closing the issue.
2. Run `dotnet list package --outdated` — review; update any package that has a newer version
   with a resolved CVE.
3. Confirm `System.Data.SqlClient` 4.8.5 resolves on net10.0 via compatibility mode.
   If it does not resolve, evaluate pinning to latest 4.x; do not replace with
   `Microsoft.Data.SqlClient` in this issue (deferred per spec non-goal).

Commit: `chore: resolve remaining package compatibility for net10.0`.

---

### Phase 6 — Docker image update

**Files changed**: `Dockerfile` (already updated in Phase 1 — verify and refine).

**Steps**:

1. Confirm the multi-stage build works end-to-end:
   ```
   docker build -t kimai-odata-reporting:net10-dev -f src/KimaiDotNet.Reporting.ODataService/Dockerfile .
   ```
2. Run the image and verify `GET /$metadata` returns a valid EDMX document:
   ```
   docker run -p 8080:80 -e Kimai__Url=... kimai-odata-reporting:net10-dev
   curl http://localhost:8080/$metadata
   ```
3. Confirm no `.NET platform compatibility` warnings in the build output.
4. Remove any `<NoWarn>` entries introduced solely to suppress upgrade-related warnings.

Commit: `chore: verify net10.0 Docker build and metadata smoke test`.

---

### Phase 7 — Test strategy and validation

#### Unit / integration tests

There is currently **no test project** in the solution (see Risk R4 below). Before the issue
can be closed, the following minimal test coverage must exist or be tracked:

| Test | Type | Priority |
|------|------|----------|
| All eight OData endpoints return HTTP 200 for valid requests | Integration | High |
| `GET /$metadata` returns valid EDMX | Integration | High |
| With chaos enabled (`InjectionRate = 1.0`), `AddChaosException` throws the configured exception type | Unit | High |
| With chaos enabled, `AddChaosLatency` delays responses by at least the configured `LatencyMs` | Unit | High |
| Configuration binds correctly from `appsettings.json` for `GeneralChaosOptions` and `OperationChaosOptions` | Unit | Medium |

If no test project exists, create `tests/KimaiDotNet.Reporting.ODataService.Tests/` as part
of this upgrade issue. The test project must target `net10.0` and use xUnit.

#### Chaos behaviour verification

To verify chaos injection works end-to-end after the Polly v8 migration:

1. Add a single `OperationChaosOptions` entry in `appsettings.Development.json` with
   `Enabled: true`, `InjectionRate: 1.0`, and `LatencyMs: 2000`.
2. Call the corresponding OData endpoint and confirm the response takes ≥ 2 seconds.
3. Set `Exception` to `"System.Net.Http.HttpRequestException"` and `InjectionRate: 1.0`.
4. Call the endpoint and confirm HTTP 500 is returned (or the configured status code).
5. Set `Enabled: false` and confirm normal response times resume.

This manual test can be codified as an xUnit integration test using
`Microsoft.AspNetCore.Mvc.Testing`.

---

## .NET 10 breaking changes

The following .NET 10 platform changes are relevant to this codebase:

| Change | Impact | Mitigation |
|--------|--------|-----------|
| Nullable reference types enforcement | `<Nullable>enable</Nullable>` is already set; .NET 10 compiler may surface new nullable warnings from updated BCL signatures | Fix each warning; do not suppress with `#pragma warning disable` |
| `System.Net.Http.HttpRequestException` constructor changes | Some constructors were deprecated in .NET 6; confirm all usages in `DependencyInjectionExtensions.cs` compile | Update to the recommended constructor overload |
| ASP.NET Core OpenAPI built-in (replaces Swashbuckle in templates) | Swashbuckle still works on .NET 10; no forced migration | No action required for this issue |
| `Microsoft.AspNetCore.OData` v9 breaking changes | Route registration and `EdmModel` registration APIs may differ | Review OData v9 changelog and update `EdmModelBuilder.cs` and `Program.cs` accordingly |
| `System.Data.SqlClient` deprecation warnings | `CreateSqlException()` in `DependencyInjectionExtensions.cs` uses `System.Data.SqlClient` via reflection; will produce deprecation warnings | Suppress only the specific `CS0618` for this file; schedule replacement via the separate tracking issue |

---

## Risk register

| ID | Risk | Likelihood | Impact | Mitigation |
|----|------|-----------|--------|-----------|
| R1 | `Microsoft.AspNetCore.OData` v9 introduces breaking controller API changes | Medium | High | Review OData v9 migration guide before Phase 3; isolate OData upgrade in its own commit |
| R2 | `MarkZither.KimaiDotNet.ApiClient` has no .NET 10–compatible release | Medium | High | Prepare a local fork/build plan before Phase 2 (see OQ2) |
| R3 | `MonkeyCache.LiteDB`/`MonkeyCache.FileStore` are unmaintained on .NET 10 | Medium | Medium | Identify `Microsoft.Extensions.Caching.Memory` as fallback before Phase 2 (see OQ3) |
| R4 | No test project exists; chaos validation cannot be automated | High | Medium | Create test project in Phase 7; do not close issue without chaos integration tests |
| R5 | Polly v8 context migration introduces subtle runtime behaviour differences in chaos injection | Low | High | Manual chaos verification in Phase 4 step-by-step before committing; add integration test in Phase 7 |
| R6 | `CreateSqlException()` uses reflection on `System.Data.SqlClient` internals; behaviour may differ on .NET 10 | Low | Low | Test with chaos `Exception = "System.Data.SqlClient.SqlError"` configuration after Phase 4 |

---

## Open items

| # | Item | Owner | Phase |
|---|------|-------|-------|
| OQ2 | Verify `MarkZither.KimaiDotNet.ApiClient` 0.4.0-beta0001 .NET 10 compatibility | Mark | Phase 2 |
| OQ3 | Verify `MonkeyCache.LiteDB` / `MonkeyCache.FileStore` 2.0.1 .NET 10 compatibility | Mark | Phase 2 |
| R4 | Create test project if none exists | Mark | Phase 7 |

---

## Commands

Executable commands for this project (copy and run directly):

### Build

```bash
dotnet build src/KimaiDotNet.Reporting.ODataService/KimaiDotNet.Reporting.ODataService.csproj --configuration Release
```

### Full solution build

```bash
dotnet build KimaiDotNet.Reporting.sln --configuration Release
```

### Tests

```bash
dotnet test KimaiDotNet.Reporting.sln --verbosity normal
```

### List packages (current versions)

```bash
dotnet list src/KimaiDotNet.Reporting.ODataService/KimaiDotNet.Reporting.ODataService.csproj package
```

### List vulnerable packages

```bash
dotnet list src/KimaiDotNet.Reporting.ODataService/KimaiDotNet.Reporting.ODataService.csproj package --vulnerable
```

### Local execution

```bash
dotnet run --project src/KimaiDotNet.Reporting.ODataService
```

### Docker build

```bash
docker build -t kimai-odata-reporting:net10-dev -f src/KimaiDotNet.Reporting.ODataService/Dockerfile .
```

### Docker metadata smoke test

```bash
docker run --rm -p 8080:80 kimai-odata-reporting:net10-dev &
sleep 5 && curl -s http://localhost:8080/\$metadata | head -20
```
