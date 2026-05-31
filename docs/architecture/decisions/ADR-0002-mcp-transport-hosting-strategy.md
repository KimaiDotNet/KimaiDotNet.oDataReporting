# ADR-0002: Co-host MCP transports alongside OData using dual-mode startup

**Status**: Accepted  
**Date**: 2026-05-31  
**Author**: Mark

---

## Context

`KimaiDotNet.Reporting.ODataService` must support two MCP transports simultaneously:

1. **HTTP transport** — an SSE or streamable-HTTP endpoint served by ASP.NET Core Kestrel,
   consumed by agent hosts that connect over a network (e.g., Claude.ai, a remote VS Code
   instance). This transport sits naturally alongside the existing OData routes in the
   ASP.NET Core request pipeline.

2. **stdio transport** — the process is launched as a subprocess by a local agent host
   (VS Code Copilot, Claude Desktop). The host writes JSON-RPC messages to the process's
   `stdin` and reads responses from `stdout`. There is no listening socket; the process
   itself _is_ the pipe.

These execution models are mutually exclusive within a single startup path:

- The ASP.NET Core web host calls `app.RunAsync()` which starts Kestrel and blocks until the
  process is shut down. While it runs, `stdout` is owned by the runtime for structured logging.
- The stdio MCP transport calls `RunMcpAsync()` (or equivalent) which consumes `stdin`/`stdout`
  for the MCP JSON-RPC protocol. Starting Kestrel alongside this would corrupt the
  MCP protocol stream because any Kestrel or logging output to `stdout` interleaves with
  MCP response payloads.

A naive "start both in the same web host" approach is therefore not feasible. An architectural
decision is required for how the single binary supports both transports.

## Decision

We will implement **dual-mode startup** controlled by a `--stdio` command-line argument.

- **Web mode** (default, no `--stdio`): `WebApplication.CreateBuilder` is used. ASP.NET Core
  starts Kestrel on the configured port. OData routes and the HTTP MCP endpoint
  (`app.MapMcp("/mcp")`) are both served on the same port. Console and structured logging
  write to `stdout` as normal.

- **stdio mode** (`--stdio` present): A generic `IHost` (no Kestrel) is built using
  `Host.CreateDefaultBuilder`. The MCP server is registered with
  `.WithStdioServerTransport()`. Console logging is redirected to `stderr` so it does not
  corrupt the `stdout` MCP stream. The host runs until the parent process closes `stdin`.

Both modes share a common DI registration helper
(`IServiceCollection.AddKimaiMcpServices()`) that registers the Kimai HTTP client and the
`KimaiMcpTools` tool class, avoiding duplication between the two startup branches.

The `--stdio` flag is the canonical argument used by VS Code's MCP server configuration
(`"args": ["--stdio"]`) and Claude Desktop's `mcpServers` JSON configuration.

## Consequences

### Positive

- A single deployable binary (or `dotnet tool`) supports both transports with no duplication
  of tool handler code.
- The HTTP MCP endpoint and OData share the same port and TLS certificate, satisfying the
  spec requirement with no additional firewall rules.
- stdio mode is a well-established subprocess pattern; VS Code Copilot and Claude Desktop
  both document it as the recommended local agent configuration.
- Logging to `stderr` in stdio mode is the correct behaviour: agent hosts typically ignore
  `stderr` unless troubleshooting, and it does not corrupt the MCP protocol.
- Shared DI means the Kimai API key safety rules (never surface in responses or logs) are
  enforced identically in both modes.

### Negative / trade-offs

- `Program.cs` has a top-level conditional branch before `WebApplication.CreateBuilder`,
  which is unconventional for ASP.NET Core services. The shared `AddKimaiMcpServices()`
  extension method mitigates the duplication risk.
- Integration-testing the stdio transport requires spawning the process as a subprocess and
  communicating over pipes, which is more complex than an in-process `WebApplicationFactory`
  test. A `StdioMcpTestHarness` helper will be needed in the test project.
- Developers running `dotnet run` locally always get web mode; they must explicitly pass
  `-- --stdio` to test stdio mode.

## Alternatives considered

### Option A: Concurrent hosting — stdio as `IHostedService`

Register a background `IHostedService` that opens `stdin`/`stdout` pipes alongside Kestrel.
Both run in the same process simultaneously.

**Rejected because**: stdio MCP requires the process's own `stdin`/`stdout` file descriptors.
Multiple components writing to `stdout` (Kestrel's request logging, the stdio MCP responder,
structured log output) would interleave, corrupting the MCP JSON-RPC protocol. The MCP
specification requires that no non-protocol bytes appear on `stdout` in stdio mode. This
option is not implementable without replacing the process's standard I/O streams at the OS
level, which is impractical and fragile.

### Option B: Separate binaries

Publish a `KimaiDotNet.Reporting.ODataService` executable (web mode only) and a separate
`KimaiDotNet.Reporting.McpStdio` executable (stdio mode only), each referencing a shared
library project for the tool handlers.

**Rejected because**: it doubles the deployment and packaging footprint. The envisioning
document targets a single `dotnet tool install -g KimaiDotNet.Reporting` command that starts
a unified server. Two binaries require two tool packages or a wrapper script, complicating
the NuGet global tool distribution model.

### Option C: Separate port for HTTP MCP

Serve HTTP MCP on a second Kestrel endpoint (different port from OData) to allow future
co-hosting with stdio on the primary port.

**Rejected because**: it does not resolve the stdio isolation problem, adds a second port
that operators must expose and firewall, and contradicts the spec's stated preference for
same-port hosting. The dual-mode approach makes a separate port unnecessary.

---

## References

- [MCP specification — transports](https://spec.modelcontextprotocol.io/specification/basic/transports/)
- [ModelContextProtocol .NET SDK — stdio transport](https://github.com/modelcontextprotocol/csharp-sdk)
- [VS Code MCP server configuration](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
- Feature spec: `docs/features/mcp-connectivity.md`
