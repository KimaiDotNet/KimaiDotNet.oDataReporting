# ADR-0003: Use `ModelContextProtocol` NuGet package as the .NET MCP server SDK

**Status**: Accepted  
**Date**: 2026-05-31  
**Author**: Mark

---

## Context

Implementing an MCP server in .NET requires a library that handles the MCP JSON-RPC
protocol, tool registration and dispatch, transport lifecycle management, and integration
with ASP.NET Core's dependency injection and request pipeline.

Three realistic options exist at the time of this decision:

1. **`ModelContextProtocol`** — Microsoft's official .NET MCP SDK, published to NuGet by
   the `modelcontextprotocol` organisation. GA'd in early 2025, developed with direct
   involvement from the .NET and ASP.NET Core teams. The companion package
   `ModelContextProtocol.AspNetCore` provides the `MapMcp()` endpoint extension and SSE /
   streamable-HTTP transport for ASP.NET Core.

2. **`McpDotNet`** — the original community-maintained .NET MCP SDK that predates
   Microsoft's package. Several early MCP proof-of-concept projects used it. It is now
   in maintenance-only mode following the release of the official SDK.

3. **Custom implementation** — hand-rolling the MCP JSON-RPC 2.0 layer, tool schema
   generation, and transport state machines.

The feature spec (non-functional requirement 1) already states: "The MCP server shall be
implemented using the `ModelContextProtocol` NuGet package (Microsoft's official .NET MCP
SDK) and `Microsoft.Extensions.AI`." This ADR records the rationale and the rejected
alternatives so that the decision is durable.

Two packages are required in combination:

| Package | Role |
|---------|------|
| `ModelContextProtocol` | Core MCP server: tool registration (`[McpServerTool]`), JSON-RPC dispatch, stdio transport, server lifecycle |
| `ModelContextProtocol.AspNetCore` | ASP.NET Core integration: `AddMcpServer()` DI extension, `MapMcp()` endpoint routing, HTTP + SSE transport |

`Microsoft.Extensions.AI` is already in widespread use across the .NET ecosystem for AI
abstractions; it is not a direct dependency of the MCP server implementation here but is
referenced by the SDK for function/tool schema generation and is listed in the spec for
completeness.

## Decision

We will use `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` as the sole MCP
server implementation packages. No community or custom MCP libraries will be added.

Specifically:

- `ModelContextProtocol` is added to `KimaiDotNet.Reporting.ODataService.csproj` at the
  latest stable version compatible with net10.0.
- `ModelContextProtocol.AspNetCore` is added for the HTTP transport and ASP.NET Core DI
  integration.
- Tool handlers are declared as public methods on a `KimaiMcpTools` class annotated with
  `[McpServerTool]`. Tool input types use standard C# records or classes; the SDK generates
  JSON Schema automatically from the public properties.
- The package is pinned to a specific minor version (e.g., `1.x`) at the time of
  implementation to guard against breaking changes, with a policy to review on each minor
  release.

## Consequences

### Positive

- First-party Microsoft support and maintenance; aligned with the .NET 10 LTS support
  lifecycle.
- Deep ASP.NET Core DI integration: `AddMcpServer().WithTools<KimaiMcpTools>()` follows
  the same pattern as `AddControllers()`, requiring no custom middleware.
- `[McpServerTool]` attribute model keeps tool registration co-located with the
  implementation — no separate registration files.
- `MapMcp()` adds the MCP HTTP endpoint as a normal ASP.NET Core route alongside OData,
  supporting the same-port hosting model decided in ADR-0002.
- Microsoft provides official documentation and a growing sample gallery; issues can be
  filed against a supported repository.

### Negative / trade-offs

- The package is relatively new (GA'd 2025); the API surface is stable but may see
  minor-version breaking changes before the 2.0 LTS boundary. Pinning the minor version
  mitigates this.
- The SDK's JSON Schema generation for tool input types relies on `System.Text.Json`
  source generation. Projects that use custom converters must verify compatibility.
- `ModelContextProtocol.AspNetCore` is a separate package from `ModelContextProtocol`,
  meaning the stdio-only mode (`--stdio` branch per ADR-0002) does not require the
  AspNetCore package, but both are added to the single project for simplicity.

## Alternatives considered

### Option A: `McpDotNet` (community SDK)

The first .NET MCP SDK, authored by the community before Microsoft published an official
package. Used in several early MCP demonstrations on GitHub.

**Rejected because**: the package entered maintenance mode following Microsoft's release of
`ModelContextProtocol`. It has no ASP.NET Core integration package, does not publish
official .NET 10 compatibility statements, and carries no Microsoft support commitment.
Adopting a superseded community package creates an unnecessary migration risk.

### Option B: Custom implementation

Implement the MCP JSON-RPC 2.0 protocol layer, tool schema generation (JSON Schema from C#
types), SSE or chunked-HTTP transport for the HTTP mode, and stdin/stdout framing for the
stdio mode from scratch.

**Rejected because**: the MCP specification is non-trivial — it spans JSON-RPC 2.0, a
capability negotiation handshake (`initialize` / `initialized`), tool schema validation, and
two distinct transport state machines. Implementing this correctly and maintaining it as the
specification evolves represents significant engineering cost with no benefit when a
first-party SDK exists. Security risk also increases with a custom JSON-RPC implementation.

---

## References

- [ModelContextProtocol NuGet package](https://www.nuget.org/packages/ModelContextProtocol)
- [ModelContextProtocol.AspNetCore NuGet package](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore)
- [ModelContextProtocol .NET SDK — GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [Microsoft Learn — Build MCP servers with .NET](https://learn.microsoft.com/en-us/dotnet/ai/get-started-mcp)
- Feature spec: `docs/features/mcp-connectivity.md`
- ADR-0002: `docs/architecture/decisions/ADR-0002-mcp-transport-hosting-strategy.md`
