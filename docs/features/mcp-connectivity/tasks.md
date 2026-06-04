# Tasks: MCP Server and Agent Connectivity

**Feature**: [#41](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/41) — MCP server and agent connectivity (workstream 2)
**Spec**: [docs/features/mcp-connectivity.md](../mcp-connectivity.md)
**Plan**: [docs/features/mcp-connectivity/plan.md](./plan.md)
**Epic**: [#3](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/3) — Epic: MCP Server (HTTP + stdio)
**ADRs**:
- [ADR-0002 — MCP Transport Hosting Strategy](../../architecture/decisions/ADR-0002-mcp-transport-hosting-strategy.md)
- [ADR-0003 — MCP SDK Selection](../../architecture/decisions/ADR-0003-mcp-sdk-selection.md)
**Date**: 2026-05-31

---

## Phase 1 — Setup: Package additions

- [x] [#42](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/42) Add `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` NuGet packages to `src/KimaiDotNet.Reporting.ODataService/KimaiDotNet.Reporting.ODataService.csproj`

## Phase 2 — Foundational: Shared DI and projection models

- [x] [#43](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/43) Implement `AddKimaiMcpServices()` shared DI extension in `src/KimaiDotNet.Reporting.ODataService/Extensions/McpServiceExtensions.cs`
- [x] [#44](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/44) Create `McpUser` and `McpTimeEntry` output projection records in `src/KimaiDotNet.Reporting.ODataService/Mcp/Models/`

## Phase 3 — US: `list_users` tool

- [ ] [#45](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/45) Implement `list_users` tool handler in `src/KimaiDotNet.Reporting.ODataService/Mcp/KimaiMcpTools.cs`
- [ ] [#46](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/46) Add xUnit tests for `list_users` tool handler in `tests/KimaiDotNet.Reporting.ODataService.Tests/Unit/Mcp/`

## Phase 4 — US: `query_time_entries` tool

- [ ] [#47](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/47) Implement `query_time_entries` tool with team fan-out in `src/KimaiDotNet.Reporting.ODataService/Mcp/KimaiMcpTools.cs`
- [ ] [#48](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/48) Add xUnit tests for `query_time_entries` tool handler (including team fan-out) in `tests/KimaiDotNet.Reporting.ODataService.Tests/Unit/Mcp/`

## Phase 5 — US: Dual-mode startup

- [ ] [#49](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/49) Implement dual-mode startup in `src/KimaiDotNet.Reporting.ODataService/Program.cs`: HTTP MCP endpoint on `/mcp` (web mode) and stdio MCP via `--stdio` flag (generic `IHost` mode)

## Phase 6 — API key safety

- [ ] [#50](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/50) Implement `Sanitise()` helper and apply it in all `KimaiMcpTools` catch blocks in `src/KimaiDotNet.Reporting.ODataService/Mcp/KimaiMcpTools.cs`
- [ ] [#51](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/51) Add unit test asserting the API key value does not appear in any MCP tool error response in `tests/KimaiDotNet.Reporting.ODataService.Tests/Unit/Mcp/`

## Phase 7 — Integration tests

- [ ] [P] [#52](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/52) Add HTTP transport integration tests (`McpHttpTransportTests.cs`) in `tests/KimaiDotNet.Reporting.ODataService.Tests/Integration/`
- [ ] [P] [#53](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/53) Add stdio transport integration test with `StdioMcpTestHarness` helper in `tests/KimaiDotNet.Reporting.ODataService.Tests/Integration/`

## Phase 8 — Configuration and documentation

- [ ] [#54](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/54) Add MCP config section to `src/KimaiDotNet.Reporting.ODataService/appsettings.json`, MCP request examples to `src/KimaiDotNet.Reporting.ODataService/kimai-odata.http`, and agent connection instructions to `README.md`

---

## Task Detail

### T01 — Add ModelContextProtocol NuGet packages

**Phase**: 1 — Setup
**Size**: S
**Dependencies**: None
**Parallelisable**: No

Add `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` NuGet packages to the
service project at the latest stable version compatible with `net10.0`. Run `dotnet restore`,
`dotnet build`, and `dotnet test` to confirm zero version conflicts with the existing
`Microsoft.Extensions.Http.Resilience` and `Polly.Core` packages. Per ADR-0003, pin the
minor version to guard against breaking changes.

**Acceptance Criteria**:
- `dotnet restore` completes with zero NU1608 or higher version-conflict warnings for MCP packages
- `dotnet build` produces zero errors
- `dotnet test` passes all existing tests without regression

**Supersedes**: Partially resolves the SDK investigation note on issue #19

---

### T02 — Implement AddKimaiMcpServices() DI extension

**Phase**: 2 — Foundational
**Size**: S
**Dependencies**: T01
**Parallelisable**: No

Create `Extensions/McpServiceExtensions.cs` with a single `AddKimaiMcpServices()` extension
method that registers `AddMcpServer().WithTools<KimaiMcpTools>()`. This shared helper is
consumed by both the web-mode and stdio-mode startup paths per ADR-0002, preventing
duplication and ensuring tool handlers are registered identically in both modes.

**Acceptance Criteria**:
- `AddKimaiMcpServices()` exists on `IServiceCollection` and calls `AddMcpServer().WithTools<KimaiMcpTools>()`
- Both web mode and stdio mode startup paths (T08) call only this extension

---

### T03 — Create MCP output projection records

**Phase**: 2 — Foundational
**Size**: S
**Dependencies**: T01
**Parallelisable**: Yes (with T02)

Create `Mcp/Models/McpUser.cs` (`record McpUser(int Id, string Username, string DisplayName)`)
and `Mcp/Models/McpTimeEntry.cs` (`record McpTimeEntry(int Id, string Begin, string End,
int Duration, string Project, string Activity, string? Description)`). These are the
simplified projections returned by MCP tools — raw Kimai API objects must never be returned
directly (secret hygiene, Phase 6).

**Acceptance Criteria**:
- Both record types exist with the fields defined in the spec contract
- No raw Kimai API response type is referenced in the `Mcp/` namespace

---

### T04 — Implement list_users tool handler

**Phase**: 3 — US: list_users
**Size**: M
**Dependencies**: T02, T03
**Parallelisable**: No

Create `Mcp/KimaiMcpTools.cs` with the `[McpServerToolType]`-annotated class. Implement
`ListUsersAsync` decorated with `[McpServerTool(Name = "list_users")]`. Delegate to the
existing Kimai API client via the named `HttpClient` (constant `Constants.HttpClients.Kimai`).
Map response objects to `McpUser` records — never return raw API types. Wrap API calls in
`try/catch` as a placeholder for Phase 6 sanitisation.

**Acceptance Criteria**:
- `tools/list` response includes `list_users` with a description matching the spec
- `list_users` returns `McpUser[]` containing at least `id`, `username`, `display_name` fields
- No raw Kimai API type is referenced in the return type or method signature

---

### T05 — Add xUnit tests for list_users

**Phase**: 3 — US: list_users
**Size**: M
**Dependencies**: T04
**Parallelisable**: No

Add `tests/KimaiDotNet.Reporting.ODataService.Tests/Unit/Mcp/KimaiMcpToolsListUsersTests.cs`.
Use a `MockHttpMessageHandler` (or `TestWebApplicationFactory`) to avoid real Kimai API
calls. Verify that `list_users` maps API response fields to `McpUser` correctly and that
the method completes without error when the Kimai API returns a valid response.

**Acceptance Criteria**:
- At least one test verifies correct `McpUser` mapping from a mocked Kimai API response
- `dotnet test` passes with the new tests included

---

### T06 — Implement query_time_entries tool with team fan-out

**Phase**: 4 — US: query_time_entries
**Size**: L
**Dependencies**: T02, T03, T04
**Parallelisable**: No

Add `QueryTimeEntriesAsync` to `KimaiMcpTools` decorated with `[McpServerTool(Name = "query_time_entries")]`.
Parameters: required `username` (string), optional `team` (string?), `dateFrom` (string?),
`dateTo` (string?). When `team` is supplied, fetch team members from the Kimai API, resolve
to usernames, and fan out the timesheet query sequentially over each member. Aggregate,
deduplicate by entry `id`, and project to `McpTimeEntry` records. Pass `begin`/`end` date
parameters directly to the Kimai API query where supported to minimise data transfer.

**Acceptance Criteria**:
- `tools/list` response includes `query_time_entries` with all four parameters documented in the schema
- Calling the tool with only `username` returns entries for that user
- Calling the tool with `team` returns aggregated entries for all team members (verified with mocked responses)
- No raw Kimai API type is returned

---

### T07 — Add xUnit tests for query_time_entries

**Phase**: 4 — US: query_time_entries
**Size**: M
**Dependencies**: T06
**Parallelisable**: No

Add `tests/KimaiDotNet.Reporting.ODataService.Tests/Unit/Mcp/KimaiMcpToolsQueryTimeEntriesTests.cs`.
Cover: (a) single-user query returns correct `McpTimeEntry` projections; (b) team fan-out
queries each team member and aggregates results; (c) date filter parameters are forwarded
to the Kimai API; (d) duplicate entries across members are deduplicated by `id`.

**Acceptance Criteria**:
- At least four test cases covering the scenarios above
- `dotnet test` passes with the new tests included

---

### T08 — Implement dual-mode startup in Program.cs

**Phase**: 5 — US: Dual-mode startup
**Size**: M
**Dependencies**: T02
**Parallelisable**: No

Modify `src/KimaiDotNet.Reporting.ODataService/Program.cs` to implement dual-mode startup
per ADR-0002. At the top of the file: if `args.Contains("--stdio")`, build a generic
`IHost` (no Kestrel) that redirects console logging to `stderr`, registers `KimaiOptions`
and the named Kimai `HttpClient`, calls `AddKimaiMcpServices()`, and calls
`.WithStdioServerTransport()`. In web mode (default): call `builder.Services.AddKimaiMcpServices()`
after existing service registrations, and call `app.MapMcp("/mcp")` after existing OData
route registration.

**Acceptance Criteria**:
- `dotnet run` starts the web server; `GET /$metadata` returns a valid EDMX document
- `dotnet run -- --stdio` starts without Kestrel; no startup logging appears on `stdout`
- All existing OData controller endpoints continue to return HTTP 200 for valid authenticated requests
- `POST /mcp` (or `/mcp/sse`) returns a valid MCP protocol handshake in web mode

---

### T09 — Implement API key sanitisation in KimaiMcpTools

**Phase**: 6 — API key safety
**Size**: M
**Dependencies**: T04, T06
**Parallelisable**: No

Inject `IOptions<KimaiOptions>` into `KimaiMcpTools`. Add a private `Sanitise(string message)`
helper that replaces the configured `Password` value with `[REDACTED]` using
`string.Replace(..., StringComparison.Ordinal)`. Wrap all Kimai API calls in each tool
handler in `try/catch`; on exception: call `Sanitise(exception.Message)`, log the sanitised
message at `Warning` level, and throw `new InvalidOperationException(sanitisedMessage)` —
never rethrow the original exception.

**Acceptance Criteria**:
- The raw API key value does not appear in any thrown exception message from `KimaiMcpTools`
- The raw API key value does not appear in any structured log output from `KimaiMcpTools` at any level
- Existing happy-path behaviour is unchanged

---

### T10 — Add unit test: API key absent from MCP error responses

**Phase**: 6 — API key safety
**Size**: S
**Dependencies**: T09
**Parallelisable**: No

Add `tests/KimaiDotNet.Reporting.ODataService.Tests/Unit/Mcp/KimaiMcpToolsSecurityTests.cs`.
Configure `KimaiMcpTools` with a known synthetic API key. Force an HTTP failure that includes
the key in the exception message (e.g., by returning it in a mock 401 response body). Assert
that the thrown `InvalidOperationException` message does not contain the synthetic key.
Also assert that no captured `ILogger` output contains the synthetic key.

**Acceptance Criteria**:
- Test passes with the synthetic key confirmed absent from exception message and log output
- `dotnet test` passes with the new test included

---

### T11 — Add HTTP transport integration tests

**Phase**: 7 — Integration tests
**Size**: M
**Dependencies**: T08
**Parallelisable**: Yes (with T12)

Add `tests/KimaiDotNet.Reporting.ODataService.Tests/Integration/McpHttpTransportTests.cs`
using the existing `TestWebApplicationFactory<Program>`. Three test cases:
(1) `ToolsList_ReturnsQueryTimeEntries_AndListUsers` — verify both tool names in response;
(2) `OData_Metadata_Unaffected_AfterMcpRegistration` — `GET /$metadata` still returns valid EDMX;
(3) `QueryTimeEntries_ApiKey_NotPresentInErrorResponse` — configure factory with invalid Kimai
URL, call `query_time_entries`, assert API key absent from response body. Use
`MockHttpMessageHandler` for Kimai API calls.

**Acceptance Criteria**:
- All three test cases pass
- `dotnet test` passes without regression to existing integration tests

---

### T12 — Add stdio transport integration test with StdioMcpTestHarness

**Phase**: 7 — Integration tests
**Size**: L
**Dependencies**: T08
**Parallelisable**: Yes (with T11)

Add `tests/KimaiDotNet.Reporting.ODataService.Tests/Integration/McpStdioTransportTests.cs`
and a `StdioMcpTestHarness` helper class. The harness launches the compiled service binary
via `System.Diagnostics.Process` with `-- --stdio`, writes a JSON-RPC `tools/list` request
to `stdin`, reads `stdout` with a timeout, and kills the process on completion/timeout.
Assert that the response contains `query_time_entries` and `list_users`. Gate the test with
`[Trait("Category", "Integration")]` so it can be conditionally excluded if the binary is
not pre-built.

**Acceptance Criteria**:
- `StdioMcpTestHarness` manages subprocess lifecycle safely (start, write, read with timeout, kill)
- Test asserts both tool names are present in the `tools/list` response
- Test does not hang indefinitely; a timeout of ≤10 s is enforced

---

### T13 — Add MCP configuration, .http examples, and README documentation

**Phase**: 8 — Configuration and documentation
**Size**: M
**Dependencies**: T08
**Parallelisable**: No

Three changes:
(1) Optionally add an `McpOptions` section to `appsettings.json` with `"Path": "/mcp"` default
so operators can change the HTTP MCP route without recompilation — no new secrets required.
(2) Add MCP request examples (`tools/list`, `tools/call` for both tools) to
`src/KimaiDotNet.Reporting.ODataService/kimai-odata.http`.
(3) Update `README.md` with: how to connect VS Code Copilot Chat (HTTP transport), how to
configure Claude Desktop (`mcpServers` JSON with `--stdio` flag), and the two exposed tools
with their input parameters.

**Acceptance Criteria**:
- `appsettings.json` contains an `McpOptions` section with documented `Path` key
- `kimai-odata.http` contains at least three MCP request examples
- `README.md` contains VS Code Copilot Chat and Claude Desktop setup instructions

---

## Reasoning Log

| Decision | Rationale |
|----------|-----------|
| Reuse existing issues #8, #19 as superseded context | New tasks (#T01–T13) are more granular and aligned with ADR-0002 and ADR-0003. Old issues pre-date the accepted ADRs. |
| No separate test tasks per plan phase | Tests are part of each task's acceptance; xUnit tasks (T05, T07, T10, T11, T12) map directly to the test phases in the plan. |
| T12 sized L (not M) | StdioMcpTestHarness subprocess management is non-trivial; the plan explicitly calls out the complexity of pipe-based process communication. |
| T06 sized L (not M) | Team fan-out logic (fetch members → resolve usernames → sequential queries → aggregate + deduplicate) spans multiple API calls and projection steps. |
| Sanitisation deferred to T09 (not bundled with T04/T06) | Plan phases 3–4 intentionally leave catch blocks as placeholders; Phase 6 adds the sanitisation consistently across all handlers at once. |
| No ADR tasks needed | ADR-0002 and ADR-0003 are both Accepted. No undocumented technical decisions were detected in the spec or plan. |
