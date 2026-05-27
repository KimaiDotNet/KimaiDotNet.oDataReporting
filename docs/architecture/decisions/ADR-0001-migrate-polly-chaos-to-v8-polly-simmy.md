# ADR-0001: Migrate Chaos Engineering from Polly v7 + Polly.Contrib.Simmy to Polly v8 (Polly.Core ≥ 8.3.0)

**Status**: Proposed  
**Date**: 2026-05-26  
**Author**: Mark

---

## Context

`KimaiDotNet.Reporting.ODataService` implements opt-in chaos engineering (fault injection,
latency injection) using:

- `Polly` 7.x — the resilience policy API (`IAsyncPolicy<T>`, `Policy<T>.Handle<E>()`,
  `IPolicyRegistry<string>`, `Polly.Context`)
- `Polly.Contrib.Simmy` 0.3.0 — chaos monkey extension built against the Polly v6/v7 API
  (`MonkeyPolicy.InjectExceptionAsync`, `MonkeyPolicy.InjectLatencyAsync`,
  `MonkeyPolicy.InjectFaultAsync`)
- `Microsoft.Extensions.Http.Polly` 7.0.3 — ASP.NET Core `IHttpClientFactory` integration
  that binds named `IAsyncPolicy<HttpResponseMessage>` entries from `IPolicyRegistry<string>`
  to named `HttpClient` instances via `AddPolicyHandlerFromRegistry`

The .NET 10 LTS upgrade (issue #15) requires resolving the compatibility of each of these
packages. Two forces are in tension:

1. **Polly v8 is a ground-up rewrite.** It replaces `IAsyncPolicy<T>` with
   `ResiliencePipeline<T>`, `IPolicyRegistry<string>` with `ResiliencePipelineRegistry<string>`
   (or DI-based `AddResiliencePipeline`), and `Polly.Context` with `ResilienceContext`. The old
   and new APIs cannot be mixed; a consumer must choose one.

2. **`Polly.Contrib.Simmy` is not maintained for Polly v8.** The last release (0.3.0, 2021)
   targets Polly v6/v7 and will not resolve against Polly v8. As of `Polly.Core` 8.3.0, chaos
   engineering strategies (`AddChaosLatency`, `AddChaosFault`, `AddChaosOutcome`,
   `AddChaosBehavior`) are included in the core package — the Polly maintainers collaborated
   with the creator of Simmy to absorb the chaos strategies directly into Polly. The
   `Polly.Simmy` NuGet package exists as the assembly that provides these types and is a
   transitive dependency of `Polly.Core` ≥ 8.3.0; no explicit separate reference is required.
   See: [Resilience and chaos engineering — .NET Blog](https://devblogs.microsoft.com/dotnet/resilience-and-chaos-engineering/).

3. **`Microsoft.Extensions.Http.Polly` is superseded.** Microsoft replaced it with
   `Microsoft.Extensions.Http.Resilience`, which integrates `IHttpClientFactory` with the Polly
   v8 pipeline API via `AddResilienceHandler`. No version of `Microsoft.Extensions.Http.Polly`
   is published for .NET 10; the last published version (8.x) still uses the Polly v7 API.

4. **The chaos configuration model must be preserved.** `GeneralChaosOptions` and
   `OperationChaosOptions` are bound from `appsettings.json`; changing their shape would break
   existing deployments. The migration must preserve all configuration key names and value
   semantics.

5. **The chaos injection pattern is context-driven.** `DependencyInjectionExtensions.cs` reads
   `OperationChaosOptions` from a `Polly.Context` at policy execution time via
   `SimmyContextExtensions`. This pattern has a direct equivalent in Polly v8 using
   `ResilienceContext.Properties` and `ResiliencePropertyKey<T>`, but requires rewriting the
   extension classes.

## Decision

We will migrate the chaos engineering subsystem to **`Polly.Core` ≥ 8.3.0**, replacing
`Microsoft.Extensions.Http.Polly` with `Microsoft.Extensions.Http.Resilience`. This follows
the approach documented in the [official .NET Blog announcement](https://devblogs.microsoft.com/dotnet/resilience-and-chaos-engineering/).

Specifically:

- `Polly` 7.x is upgraded to `Polly.Core` ≥ 8.3.0 (pinned explicitly to ensure chaos
  strategies are available; `Polly.Simmy` is included transitively).
- `Polly.Contrib.Simmy` 0.3.0 is removed. No explicit `Polly.Simmy` NuGet reference is
  added; chaos types are available via `Polly.Core` ≥ 8.3.0.
- `Microsoft.Extensions.Http.Polly` 7.0.3 is removed and replaced by
  `Microsoft.Extensions.Http.Resilience` (latest stable compatible with net10.0).
- `Microsoft.Extensions.Http` is retained (or superseded by the version bundled with the
  ASP.NET Core 10 meta-package as appropriate).
- `GeneralChaosOptions` and `OperationChaosOptions` are not modified; all configuration key
  names and value semantics are preserved.
- `DependencyInjectionExtensions.cs`, `SimmyContextExtensions.cs`, and
  `PollyContextExtensions.cs` are rewritten to the Polly v8 API surface
  (`ResiliencePipeline<T>`, `ResilienceContext`, `ResiliencePropertyKey<T>`).
- The context-driven chaos pattern (per-operation enable/rate/exception resolved at execution
  time from `IOptions<GeneralChaosOptions>`) is preserved using Polly v8
  `ChaosExceptionStrategyOptions.EnabledGenerator`, `InjectionRateGenerator`, and
  `ExceptionGenerator` delegates.
- `Program.cs` policy registration is rewritten using `builder.Services.AddResiliencePipeline`
  and the `AddResilienceHandler` extension from `Microsoft.Extensions.Http.Resilience`.

## Consequences

### Positive

- All three packages (`Polly.Contrib.Simmy`, `Microsoft.Extensions.Http.Polly`, old `Polly`)
  are replaced by actively maintained, .NET 10–native equivalents with first-party support.
- Chaos strategies are now part of `Polly.Core` ≥ 8.3.0: `AddChaosLatency`,
  `AddChaosFault`, `AddChaosOutcome`, and `AddChaosBehavior` are direct replacements for
  `MonkeyPolicy.InjectLatencyAsync`, `MonkeyPolicy.InjectFaultAsync`,
  `MonkeyPolicy.InjectExceptionAsync`, and equivalent Simmy behaviour injection.
- `Microsoft.Extensions.Http.Resilience` adds built-in observability (OpenTelemetry metrics and
  tracing) with no additional configuration.
- The Polly v8 API is strongly typed and uses source-generator–friendly patterns, reducing
  runtime reflection.
- No configuration schema changes are required for existing deployments.

### Negative / trade-offs

- **High implementation effort**: the Polly v8 API is a breaking change from v7. Every callsite
  in `DependencyInjectionExtensions.cs` and `Program.cs` must be rewritten. Estimated scope:
  3–4 files, ~150 lines of Polly-specific code.
- **No direct upgrade path**: `Polly.Contrib.Simmy` and `Polly` v7 must be removed entirely
  before `Polly` v8 and `Polly.Simmy` can be added; incremental migration is not possible
  within a single project.
- **`IPolicyRegistry<string>` iteration pattern is not supported in v8**: the current
  `DependencyInjectionExtensions.AddHttpChaosInjectors` method wraps existing registry entries
  after the fact. In v8, chaos strategies must be embedded in the pipeline at registration
  time. This requires reorganising the pipeline registration sequence in `Program.cs`.
- **`Polly.Context.OperationKey` semantic changes**: in v8, `ResilienceContext.OperationKey`
  is a `string?` and `ResilienceContext.Properties` replaces the dictionary-style context.
  Extension methods in `SimmyContextExtensions.cs` must be updated to use
  `ResiliencePropertyKey<GeneralChaosOptions>`.

## Alternatives considered

### Option A: Keep Polly v7 + Polly.Contrib.Simmy; find an alternative HTTP integration

`Polly` 7.x and `Polly.Contrib.Simmy` 0.3.0 both target `netstandard2.0`, which .NET 10
supports via compatibility shim. In principle the libraries would load. However,
`Microsoft.Extensions.Http.Polly` has no published version for .NET 10, and the last published
version (8.x) bundles `Microsoft.Extensions.Http` 8.x, creating a version conflict when
targeting `net10.0`. Manually wiring `IHttpClientFactory` with a Polly v7 registry is
possible but requires maintaining undocumented integration code that Microsoft has explicitly
deprecated. This path defers the migration and accumulates further debt.

**Rejected because**: leaves the codebase on a deprecated, unsupported API with no future
upgrade path; adds undocumented custom integration code; does not resolve the root dependency
conflict with `Microsoft.Extensions.Http.Polly`.

### Option B: Stub chaos to a no-op for this upgrade cycle; reimplement in a follow-up issue

Remove `Polly.Contrib.Simmy` and all chaos injection calls, replacing them with no-op
delegates or conditional `#if DEBUG` guards, and track chaos re-implementation as a separate
issue. This minimises the scope of the .NET 10 upgrade issue itself.

**Rejected because**: chaos engineering is listed as an explicit functional requirement in the
feature spec (requirement F.3) and an acceptance criterion. Disabling it would leave the
service non-compliant with the spec and require a second upgrade iteration. The migration
effort to Polly v8 is bounded and well-understood; deferring it adds more total effort,
not less.

### Option C: Replace chaos engineering with a third-party alternative (e.g., Chaos Toolkit, Chaos Monkey for .NET)

Adopt a non-Polly chaos library that is natively compatible with .NET 10 and does not require
migrating the resilience pipeline API.

**Rejected because**: the existing chaos configuration model (`GeneralChaosOptions`,
`OperationChaosOptions`) is tightly coupled to Polly's context-passing mechanism. Adopting a
different library would require changing the configuration schema, rewriting the entire
integration layer, and potentially breaking existing `appsettings.json` files in deployed
instances. `Polly.Core` ≥ 8.3.0 (with built-in chaos strategies) is the lowest-friction path
that preserves the existing behaviour contract and follows the official Microsoft recommendation.

---

## References

- [Resilience and chaos engineering — .NET Blog (Feb 2024)](https://devblogs.microsoft.com/dotnet/resilience-and-chaos-engineering/) — official announcement of chaos strategies in Polly.Core 8.3.0
- [Polly v8 migration guide](https://www.thepollyproject.org/2023/09/28/polly-v8-migration-guide/)
- [Polly.Core NuGet package](https://www.nuget.org/packages/Polly.Core)
- [Polly.Simmy NuGet package](https://www.nuget.org/packages/Polly.Simmy) — transitive via Polly.Core ≥ 8.3.0
- [Microsoft.Extensions.Http.Resilience documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- Feature spec: [docs/features/upgrade-to-net10.md](../../features/upgrade-to-net10.md)
- GitHub Issue: [#15 — Upgrade ODataService project to net10.0](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/15)
