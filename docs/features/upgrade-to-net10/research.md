# Research: Upgrade OData Service to .NET 10 LTS

**Feature**: Upgrade OData Service to .NET 10 LTS  
**Issue**: [#15](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/15)  
**Date**: 2026-05-26

---

## Decisions made

| # | Decision | Outcome | Reference |
|---|----------|---------|-----------|
| 1 | Chaos engineering migration path | Migrate to `Polly.Core` ≥ 8.3.0 (chaos built-in); remove `Polly.Contrib.Simmy` and `Microsoft.Extensions.Http.Polly`; replace with `Microsoft.Extensions.Http.Resilience`. No separate `Polly.Simmy` reference needed | ADR-0001 |
| 2 | `Microsoft.AspNetCore.OData` version | Upgrade to 9.x — v8.x does not declare `net10.0` as a supported TFM; v9.x is the current release with .NET 10 support | Open question #5 resolved |
| 3 | Polly context migration | Replace `Polly.Context` / `IPolicyRegistry<string>` with `ResilienceContext` / `ResiliencePipelineRegistry<string>` throughout; chaos is embedded at pipeline registration time rather than wrapped post-hoc | ADR-0001 |
| 4 | `System.Data.SqlClient` deferral | No action in this issue; the package targets `netstandard2.0` and resolves on .NET 10 via compatibility mode; replacement with `Microsoft.Data.SqlClient` is tracked separately | Spec non-goal |
| 5 | Swashbuckle | .NET 10 ships `Microsoft.AspNetCore.OpenApi` built-in; Swashbuckle 7.x supports .NET 10. Keep Swashbuckle for now; evaluate migration to built-in OpenAPI as a follow-up | Low risk |

## Open items (unresolved at plan time)

| # | Question | Risk if unresolved | Recommended first action |
|---|----------|--------------------|--------------------------|
| OQ2 | `MarkZither.KimaiDotNet.ApiClient` 0.4.0-beta0001 .NET 10 compatibility | Build fails if the package does not resolve on net10.0 | Run `dotnet list package --include-prerelease` after TFM change; if restore fails, clone the source repo and build a local package |
| OQ3 | `MonkeyCache.LiteDB` / `MonkeyCache.FileStore` 2.0.1 .NET 10 compatibility | Caching layer fails at runtime if the packages have .NET 10 incompatibilities | Check NuGet for newer versions; review the package source for last-commit date; if unmaintained, evaluate `Microsoft.Extensions.Caching.Memory` as a drop-in replacement |

## Chaos engineering subsystem — current API inventory

The following Polly v7 / Simmy API calls must be migrated as part of Phase 4.

### Program.cs

| Current (v7) | Polly v8 equivalent |
|---|---|
| `MonkeyPolicy.InjectLatencyAsync(with => with.Latency(...).InjectionRate(...).Enabled())` | `new ResiliencePipelineBuilder<HttpResponseMessage>().AddChaosLatency(new ChaosLatencyStrategyOptions { ... })` |
| `MonkeyPolicy.InjectFaultAsync<HttpResponseMessage>(ex, rate, enabled)` | `AddChaosException(new ChaosExceptionStrategyOptions { ... })` |
| `Policy<HttpResponseMessage>.Handle<Exception>().WaitAndRetryAsync(...)` | `AddRetry(new RetryStrategyOptions<HttpResponseMessage> { ... })` |
| `builder.Services.AddPolicyRegistry()` → `IPolicyRegistry<string>` | `builder.Services.AddResiliencePipeline<string, HttpResponseMessage>("key", builder => { ... })` |
| `.AddPolicyHandlerFromRegistry("WrappedChoas")` | `.AddResilienceHandler("WrappedResilience")` (from `Microsoft.Extensions.Http.Resilience`) |

### DependencyInjectionExtensions.cs

| Current (v7) | Polly v8 equivalent |
|---|---|
| `AddHttpChaosInjectors(this IPolicyRegistry<string> registry)` — wraps each `IAsyncPolicy<HttpResponseMessage>` post-registration | Chaos strategies are embedded in the pipeline during `AddResiliencePipeline` registration; `AddHttpChaosInjectors` is removed |
| `MonkeyPolicy.InjectExceptionAsync(with => with.Fault(GetException).InjectionRate(GetInjectionRate).EnabledWhen(GetEnabled))` | `AddChaosException(new ChaosExceptionStrategyOptions { EnabledGenerator = GetEnabled, InjectionRateGenerator = GetInjectionRate, ExceptionGenerator = GetException })` |
| `MonkeyPolicy.InjectLatencyAsync(with => with.Latency(GetLatency).InjectionRate(GetInjectionRate).EnabledWhen(GetEnabled))` | `AddChaosLatency(new ChaosLatencyStrategyOptions { EnabledGenerator = GetEnabled, InjectionRateGenerator = GetInjectionRate, LatencyGenerator = GetLatency })` |
| `context.GetOperationChaosSettings()` reading from `Polly.Context` | `args.Context.Properties.TryGetValue(ChaosSettingsKey, out var settings)` where `ChaosSettingsKey` is `ResiliencePropertyKey<GeneralChaosOptions>` |

### SimmyContextExtensions.cs

| Current (v7) | Polly v8 equivalent |
|---|---|
| `context.WithChaosSettings(options)` → `context[ChaosSettings] = options` | `context.Properties.Set(ChaosSettingsKey, options)` |
| `context.GetChaosSettings()` → `context[ChaosSettings] as GeneralChaosOptions` | `context.Properties.TryGetValue(ChaosSettingsKey, out var settings)` |
| `Polly.Context` parameter type | `ResilienceContext` parameter type |

### PollyContextExtensions.cs

| Current (v7) | Polly v8 equivalent |
|---|---|
| `Context.WithLogger<T>(ILogger)` | `ResilienceContext.Properties.Set(LoggerKey, logger)` where `LoggerKey = new ResiliencePropertyKey<ILogger>("ILogger")` |
| `Context.GetLogger()` | `ResilienceContext.Properties.TryGetValue(LoggerKey, out var logger)` |

## Package resolution summary

| Package | Current | Planned version | Notes |
|---------|---------|----------------|-------|
| `Polly` | 7.x (transitive) | — | Removed; superseded by `Polly.Core` |
| `Polly.Core` | — | ≥ 8.3.0 (explicit) | New; chaos strategies built-in from 8.3.0; `Polly.Simmy` included transitively |
| `Polly.Contrib.Simmy` | 0.3.0 | **Removed** | Replaced by built-in chaos in `Polly.Core` ≥ 8.3.0 |
| `Polly.Simmy` | — | transitive | Included via `Polly.Core` ≥ 8.3.0; no explicit reference needed |
| `Microsoft.Extensions.Http.Polly` | 7.0.3 | **Removed** | Replaced by Microsoft.Extensions.Http.Resilience |
| `Microsoft.Extensions.Http.Resilience` | — | latest net10.0-compatible | New; provides `AddResilienceHandler` |
| `Microsoft.Extensions.Http` | 7.0.0 | latest (or framework-provided) | Low risk |
| `Microsoft.AspNetCore.OData` | 8.0.12 | 9.x latest stable | v8 does not support net10.0 |
| `MiniProfiler.AspNetCore.Mvc` | 4.2.22 | 4.3.x or 5.x | Confirm net10.0 TFM support |
| `Microsoft.OpenApi.OData` | 1.2.0 | latest 1.x | Targets netstandard2.0; low risk |
| `Swashbuckle.AspNetCore` | 6.5.0 | 7.x | .NET 10 compatible |
| `CsvHelper` | 30.0.1 | latest (if needed) | Targets netstandard2.0; low risk |
| `MonkeyCache.LiteDB` / `MonkeyCache.FileStore` | 2.0.1 | TBD — see OQ3 | Investigate before Phase 5 |
| `MarkZither.KimaiDotNet.ApiClient` | 0.4.0-beta0001 | TBD — see OQ2 | Investigate before Phase 2 |
| `System.Data.SqlClient` | 4.8.5 | 4.8.5 (no change) | netstandard2.0; deferred |
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | 1.17.2 | latest stable | Dev-time only; low risk |
