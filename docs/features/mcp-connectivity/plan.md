# Plan: MCP Server and Agent Connectivity

**Feature**: MCP server and agent connectivity (workstream 2)  
**Spec**: [docs/features/mcp-connectivity.md](../mcp-connectivity.md)  
**Issue**: TBD  
**Epic**: TBD — MCP server (workstream 2)  
**ADRs**:
- [ADR-0002 — MCP Transport Hosting Strategy](../../architecture/decisions/ADR-0002-mcp-transport-hosting-strategy.md)
- [ADR-0003 — MCP SDK Selection](../../architecture/decisions/ADR-0003-mcp-sdk-selection.md)  

**Date**: 2026-05-31

---

## Implementation sequence

The feature is delivered in seven sequential phases. Each phase must leave the project in a
buildable, passing-test state before the next phase begins. Commit after each phase.

---

### Phase 1 — Package additions

**Files changed**: `KimaiDotNet.Reporting.ODataService.csproj`

**Steps**:

1. Add `ModelContextProtocol` at the latest stable version compatible with net10.0:
   ```
   dotnet add src/KimaiDotNet.Reporting.ODataService package ModelContextProtocol
   dotnet add src/KimaiDotNet.Reporting.ODataService package ModelContextProtocol.AspNetCore
   ```
2. Run `dotnet restore` and confirm clean resolution — no version conflicts with existing
   `Microsoft.Extensions.Http.Resilience` or `Polly.Core`.
3. Run `dotnet build` and confirm zero errors.
4. Run `dotnet test` and confirm the existing test suite passes without regression.

> **Note**: `Microsoft.Extensions.AI` is a transitive dependency of `ModelContextProtocol`;
> do not add an explicit reference unless a direct type reference is needed.

Commit: `chore: add ModelContextProtocol and ModelContextProtocol.AspNetCore packages`.

---

### Phase 2 — Shared DI extension

**Files changed**: `Extensions/McpServiceExtensions.cs` (new)

**Objective**: Extract the MCP-specific DI registrations into a single extension method
consumed by both the web-mode and stdio-mode startup paths (per ADR-0002). This prevents
duplication in `Program.cs` and ensures the Kimai HTTP client and tool handlers are
registered identically in both modes.

**Steps**:

1. Create `Extensions/McpServiceExtensions.cs`:

   ```csharp
   namespace MarkZither.KimaiDotNet.Reporting.ODataService.Extensions;

   public static class McpServiceExtensions
   {
       public static IServiceCollection AddKimaiMcpServices(this IServiceCollection services)
       {
           services.AddMcpServer()
               .WithTools<KimaiMcpTools>();
           return services;
       }
   }
   ```

2. Do not register the Kimai `HttpClient` here — it is already registered in the web-mode
   path. The stdio mode must register it separately (see Phase 5).

Commit: `feat: add McpServiceExtensions with AddKimaiMcpServices registration`.

---

### Phase 3 — Tool handler: `list_users`

**Files changed**: `Mcp/KimaiMcpTools.cs` (new), `Mcp/Models/McpUser.cs` (new)

**Objective**: Implement the simpler of the two tools first to establish the tool pattern
and verify the MCP server registers and returns `tools/list` correctly before adding the
more complex fan-out logic.

**Steps**:

1. Create the output projection record `Mcp/Models/McpUser.cs`:

   ```csharp
   namespace MarkZither.KimaiDotNet.Reporting.ODataService.Mcp.Models;

   public record McpUser(int Id, string Username, string DisplayName);
   ```

2. Create `Mcp/KimaiMcpTools.cs` with the `list_users` tool:

   ```csharp
   using ModelContextProtocol.Server;

   namespace MarkZither.KimaiDotNet.Reporting.ODataService.Mcp;

   [McpServerToolType]
   public sealed class KimaiMcpTools(IHttpClientFactory httpClientFactory)
   {
       [McpServerTool(Name = "list_users",
           Description = "Returns the list of users known to the Kimai instance.")]
       public async Task<IReadOnlyList<McpUser>> ListUsersAsync(CancellationToken ct)
       {
           // Delegate to MarkZither.KimaiDotNet.ApiClient via named HttpClient
           // Map to McpUser projection — never return raw API response
           // Scrub any exception message before re-throwing (see Phase 6)
       }
   }
   ```

3. Wire the HTTP client inside the method using `httpClientFactory.CreateClient(Constants.HttpClients.Kimai)`.
   Use the `MarkZither.KimaiDotNet.ApiClient` types already imported by the existing
   controllers — do not add a second raw `HttpClient` call.

4. Map API response objects to `McpUser` records. Do not pass through raw Kimai response
   objects (they may contain the API key in auth context fields).

Commit: `feat: add KimaiMcpTools with list_users tool`.

---

### Phase 4 — Tool handler: `query_time_entries`

**Files changed**: `Mcp/KimaiMcpTools.cs`, `Mcp/Models/McpTimeEntry.cs` (new)

**Objective**: Implement `query_time_entries` including the team fan-out logic.

**Steps**:

1. Create `Mcp/Models/McpTimeEntry.cs`:

   ```csharp
   namespace MarkZither.KimaiDotNet.Reporting.ODataService.Mcp.Models;

   public record McpTimeEntry(
       int Id,
       string Begin,
       string End,
       int Duration,
       string Project,
       string Activity,
       string? Description);
   ```

2. Add the `query_time_entries` tool method to `KimaiMcpTools`:

   ```csharp
   [McpServerTool(Name = "query_time_entries",
       Description = "Returns time entries for the specified user, optionally filtered by team, start date, and end date.")]
   public async Task<IReadOnlyList<McpTimeEntry>> QueryTimeEntriesAsync(
       [Description("Kimai username of the user whose entries to query")] string username,
       [Description("Kimai team name; when supplied, entries for all team members are returned")] string? team,
       [Description("Inclusive lower bound for the entry start date (ISO 8601)")] string? dateFrom,
       [Description("Inclusive upper bound for the entry start date (ISO 8601)")] string? dateTo,
       CancellationToken ct)
   {
       // 1. If team is supplied: fetch team members, resolve to usernames, fan out
       // 2. Query timesheets for each username (or just username if no team)
       // 3. Aggregate and deduplicate
       // 4. Project to McpTimeEntry records
   }
   ```

3. The fan-out loop calls the Kimai API once per team member. Keep calls sequential to avoid
   overwhelming a self-hosted Kimai instance; parallelism is not required by the spec.

4. Apply date filter parameters directly as query parameters to the Kimai API (`begin`, `end`)
   where the API supports them, to minimise data transfer.

Commit: `feat: add query_time_entries tool with team fan-out`.

---

### Phase 5 — Dual-mode startup in `Program.cs`

**Files changed**: `Program.cs`

**Objective**: Implement the dual-mode startup per ADR-0002. Web mode adds the HTTP MCP
endpoint alongside OData. stdio mode starts a generic host with no Kestrel.

**Steps**:

1. At the very top of `Program.cs`, before `WebApplication.CreateBuilder`, add the
   `--stdio` branch:

   ```csharp
   if (args.Contains("--stdio"))
   {
       // stdio mode — no web server, no Kestrel
       // Redirect console logging to stderr so stdout is clean for MCP protocol
       var stdioHost = Host.CreateDefaultBuilder(args)
           .ConfigureLogging(logging =>
           {
               logging.ClearProviders();
               logging.AddConsole(opts => opts.FormatterName = "simple");
               // Route to stderr
           })
           .ConfigureServices((ctx, services) =>
           {
               services.AddOptions<KimaiOptions>()
                   .Bind(ctx.Configuration.GetSection(KimaiOptions.Key));
               KimaiOptions kimaiOptions = new();
               ctx.Configuration.GetSection(KimaiOptions.Key).Bind(kimaiOptions);
               services.AddHttpClient(Constants.HttpClients.Kimai, httpClient =>
               {
                   httpClient.BaseAddress = new Uri(kimaiOptions.Url);
                   httpClient.DefaultRequestHeaders.Authorization =
                       new System.Net.Http.Headers.AuthenticationHeaderValue(
                           "Bearer", kimaiOptions.Password);
               });
               services.AddKimaiMcpServices();
               services.AddMcpServer().WithStdioServerTransport();
           })
           .Build();
       await stdioHost.RunAsync();
       return;
   }
   ```

2. In the web-mode path (existing `WebApplication.CreateBuilder` block), add after OData
   registration:

   ```csharp
   builder.Services.AddKimaiMcpServices();
   ```

3. After `var app = builder.Build()`, add the MCP HTTP endpoint:

   ```csharp
   app.MapMcp("/mcp");
   ```

4. Verify OData routes still respond: run `dotnet run` and confirm `GET /$metadata` returns
   a valid EDMX document.

5. Verify MCP HTTP endpoint: use the `.http` scratch file or `curl` to confirm
   `POST /mcp/sse` or `GET /mcp` responds with an MCP protocol handshake.

Commit: `feat: add dual-mode startup — HTTP MCP on /mcp route and stdio MCP via --stdio flag`.

---

### Phase 6 — API key safety

**Files changed**: `Mcp/KimaiMcpTools.cs`, `Mcp/McpToolExceptionHandler.cs` (new, if needed)

**Objective**: Ensure the Kimai API key (`KimaiOptions.Password`) never appears in any MCP
tool response body, error message, or server log. This is a non-functional acceptance
criterion from the spec.

**Steps**:

1. Read `KimaiOptions` in `KimaiMcpTools` (inject `IOptions<KimaiOptions>`).

2. Add a private `Sanitise(string message)` helper that replaces the API key value with
   `[REDACTED]` using `string.Replace` with `StringComparison.Ordinal`. Use this in every
   `catch` block before constructing an error string or re-throwing:

   ```csharp
   private string Sanitise(string message) =>
       _kimaiOptions.Value.Password is { Length: > 0 } key
           ? message.Replace(key, "[REDACTED]", StringComparison.Ordinal)
           : message;
   ```

3. Wrap all Kimai API calls in `try/catch`. On exception:
   - Sanitise `exception.Message` via `Sanitise()`.
   - Log the sanitised message at `Warning` level (never `Debug`/`Trace` with raw exception).
   - Throw a new `InvalidOperationException(sanitisedMessage)` — do not rethrow the original
     exception as it may carry raw HTTP response details in its inner chain.

4. Add a unit test that constructs a `KimaiMcpTools` instance with a known API key value,
   triggers an HTTP failure that includes the key in the exception message, and asserts that
   the thrown exception message does not contain the key.

Commit: `feat: sanitise API key from MCP tool error messages and logs`.

---

### Phase 7 — Integration tests

**Files changed**: `tests/KimaiDotNet.Reporting.ODataService.Tests/Integration/` (new test files)

**Objective**: Verify both transport acceptance criteria from the spec using the existing
`TestWebApplicationFactory` pattern.

**Test cases**:

#### HTTP transport tests (`McpHttpTransportTests.cs`)

1. `ToolsList_ReturnsQueryTimeEntries_AndListUsers` — calls `tools/list` via HTTP and asserts
   both tool names are present in the response.
2. `OData_Metadata_Unaffected_AfterMcpRegistration` — calls `GET /$metadata` and asserts a
   valid EDMX document is returned (regression guard per acceptance criteria).
3. `QueryTimeEntries_ApiKey_NotPresentInErrorResponse` — configures the factory with an
   invalid Kimai URL to force an error, calls `query_time_entries`, and asserts the
   configured API key value does not appear in the response body.

#### stdio transport tests (`McpStdioTransportTests.cs`)

4. `StdioMode_ToolsList_ReturnsExpectedTools` — spawns `dotnet run -- --stdio` as a
   subprocess via `System.Diagnostics.Process`, sends a JSON-RPC `tools/list` request to
   `stdin`, reads `stdout`, and asserts the response contains `query_time_entries` and
   `list_users`.

   A `StdioMcpTestHarness` helper class manages the subprocess lifecycle (start, write,
   read with timeout, kill).

**Constraints**:

- HTTP transport tests use `TestWebApplicationFactory<Program>` (already in the test project).
- All tests use `MockHttpMessageHandler` (or equivalent) to avoid real Kimai API calls.
- The stdio subprocess test requires `dotnet build` to have run first; gate it with a
  `[Trait("Category", "Integration")]` skip condition or use a build-output path constant.

Commit: `test: add MCP HTTP and stdio transport integration tests`.

---

### Phase 8 — Configuration and documentation

**Files changed**: `appsettings.json`, `appsettings.Development.json`, `kimai-odata.http`,
`README.md`

**Steps**:

1. Optionally add an `McpOptions` section to `appsettings.json` for the HTTP MCP path
   (default `/mcp`), to allow operators to change the route without recompilation. No new
   secrets are required — the existing `KimaiOptions` covers all authentication.

2. Add MCP request examples to `kimai-odata.http`:
   ```http
   ### MCP tools/list
   POST {{baseUrl}}/mcp
   Content-Type: application/json

   {"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}
   ```

3. Update `README.md` to document:
   - How to connect VS Code Copilot Chat to the MCP server (HTTP transport).
   - How to configure Claude Desktop to launch the service in stdio mode.
   - The two exposed MCP tools and their input parameters.

Commit: `docs: add MCP connection instructions and appsettings documentation`.

---

## Engineering Practices

| Practice | Decision | Reference |
|----------|----------|-----------|
| Branch strategy | Feature branch off `main`: `feature/mcp-connectivity` | Established convention |
| Commit style | Conventional Commits (`feat:`, `fix:`, `test:`, `docs:`, `chore:`) | Established convention |
| Tests | xUnit; integration tests mirror source namespace | Established convention |
| Security | API key sanitisation in tool error paths | Non-functional requirement in spec; Phase 6 |

---

## Commands

Executable commands for this project (copy and run directly):

### Build

```
dotnet build KimaiDotNet.Reporting.sln
```

### Tests

```
dotnet test KimaiDotNet.Reporting.sln --verbosity normal
```

### Local execution — web mode (OData + HTTP MCP)

```
dotnet run --project src/KimaiDotNet.Reporting.ODataService
```

### Local execution — stdio mode

```
dotnet run --project src/KimaiDotNet.Reporting.ODataService -- --stdio
```

### Verify MCP HTTP endpoint (requires running service)

```
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}'
```
