# Feature spec: MCP server and agent connectivity

**Status**: Draft
**Author**: Mark
**Created**: 2026-05-31
**Last updated**: 2026-05-31
**Version**: 1.1
**GitHub Issue**: TBD
**Epic**: TBD — MCP server (workstream 2)
**Related ADRs**: None

---

## Summary

`KimaiDotNet.Reporting.ODataService` currently exposes Kimai time-tracking data only over OData, which is inaccessible to AI agents and conversational tools. This feature adds a Model Context Protocol (MCP) server to the service, supporting both HTTP and stdio transports and exposing a set of MCP tools that allow agents to query time entries by user or team. The MCP server runs in-process alongside the existing OData endpoints on the same port, delegates all Kimai API calls to the existing `MarkZither.KimaiDotNet.ApiClient`, and is implemented using the official .NET `ModelContextProtocol` NuGet package. Creating time entries via MCP is out of scope for this phase and deferred to a later workstream. This directly addresses pain point T2 from the envisioning document.

## Goals

- Add an MCP server that responds correctly to `tools/list`, `tools/call`, and `initialize` requests over both HTTP and stdio transports.
- Expose a `query_time_entries` tool that allows agents to query time entries by user or team with optional date filtering.
- Expose a `list_users` tool so agents can discover valid Kimai usernames without prior knowledge.
- Reuse the existing `MarkZither.KimaiDotNet.ApiClient` for all Kimai API interactions — no second API client.
- Provide integration tests that verify both transports against the running service.

## Non-goals

- Creating time entries via MCP (deferred to a later workstream).
- Writing back to Kimai via the existing OData layer (OData remains read-only).
- Non-.NET MCP server implementations.
- Adding new OData entity sets or modifying the EDM model.
- Implementing Azure DevOps calendar integration (workstream 5).
- Defining a separate authentication scheme for MCP clients (reuses existing Kimai API key configuration).
- MCP sampling / agent interactivity (deferred along with `create_time_entry`).

## Requirements

### Functional

1. The system shall host an MCP server endpoint supporting the HTTP transport (HTTP + SSE or streamable HTTP, per MCP specification) on the same port as the existing OData endpoints.
2. The system shall host an MCP server endpoint supporting the stdio transport, enabling use as a subprocess by agent hosts such as VS Code Copilot and Claude Desktop.
3. The system shall respond to `tools/list` requests on both transports, returning the full list of available tools with their input schemas.
4. The system shall expose a `query_time_entries` tool that accepts a required `username` parameter and optional `team`, `date_from`, and `date_to` parameters, and returns a simplified projection of time entries matching the criteria.
5. The system shall expose a `list_users` tool that returns the list of users known to the Kimai instance, enabling agents to resolve usernames to valid identifiers.
6. The system shall delegate all Kimai API calls through the existing `MarkZither.KimaiDotNet.ApiClient`, with no direct HTTP calls to the Kimai API from MCP tool handler code.
7. The `query_time_entries` tool shall perform a server-side fan-out over team members when a `team` parameter is supplied, aggregating results from all team member queries.

### Non-functional

1. The MCP server shall be implemented using the `ModelContextProtocol` NuGet package (Microsoft's official .NET MCP SDK) and `Microsoft.Extensions.AI`.
2. The Kimai API key shall not appear in any MCP tool response body, error message, or server log output at any log level.
3. The MCP server shall start and accept connections without requiring any additional configuration keys beyond those already present in `appsettings.json` (transport endpoints may be added, but no new secrets are required).
4. All existing OData endpoints shall continue to operate without regression after the MCP server is added.

## MCP tool contract

### `query_time_entries`

| Property | Value |
|----------|-------|
| Description | Returns time entries for the specified user, optionally filtered by team, start date, and end date. |

**Input schema**

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `username` | string | Yes | Kimai username of the user whose entries to query |
| `team` | string | No | Kimai team name; when supplied, entries for all team members are returned |
| `date_from` | string (ISO 8601 date) | No | Inclusive lower bound for the entry start date |
| `date_to` | string (ISO 8601 date) | No | Inclusive upper bound for the entry start date |

**Output**: Array of simplified time entry objects containing `id`, `begin`, `end`, `duration` (seconds), `project`, `activity`, `description`.

---

### `list_users`

**Output**: Array of user objects containing `id`, `username`, `display_name`.

## Acceptance criteria

- [ ] `tools/list` over HTTP transport returns at minimum `query_time_entries` and `list_users` — verified by an integration test.
- [ ] `tools/list` over stdio transport returns the same tool list — verified by an integration test.
- [ ] `query_time_entries` returns the correct simplified time entry projections for a named user in a manual test against a live Kimai instance.
- [ ] `query_time_entries` with a `team` parameter returns aggregated entries for all members of that team.
- [ ] The Kimai API key does not appear in any MCP response body or server log at any log level — verified by a log-scraping test.
- [ ] All existing OData controller endpoints (`/Activities`, `/Customers`, `/Exports`, `/Projects`, `/Teams`, `/TeamMemberships`, `/Timesheets`, `/Users`) continue to return HTTP 200 for valid authenticated requests.
- [ ] `GET /$metadata` continues to return a valid EDMX document after the MCP server is added.
- [ ] `dotnet test` passes all new MCP integration tests on .NET 10.
- [ ] The MCP server can be configured and started without adding any new secrets to `appsettings.json`.

## Open questions

| # | Question | Owner | Status |
|---|----------|-------|--------|
| 1 | Does MCP sampling (agent interactivity / elicitation) meet UX needs for the create-entry workflow, or is a prompt-loop in the agent client sufficient? | Mark | Resolved: deferred — `create_time_entry` is out of scope for this phase |
| 2 | Will both OData and MCP endpoints be served on the same port, or separate ports? | Mark | Resolved: same port; separate port acceptable if implementation complexity warrants it |
| 3 | Should `query_time_entries` return raw Kimai timesheet objects or a simplified projection? | Mark | Resolved: simplified projection |

## Dependencies

- **Workstream 1 — .NET 10 LTS upgrade**: Must be complete before this feature is implemented. ✅ Done.
- **`MarkZither.KimaiDotNet.ApiClient`**: Already in use by existing controllers; must expose `GET /timesheets` and `GET /users` operations accessible from MCP tool handlers.
- **`ModelContextProtocol` NuGet package**: Microsoft's official .NET MCP SDK; must be available on NuGet and support .NET 10.
- **`Microsoft.Extensions.AI` NuGet package**: Required for MCP server hosting integration with ASP.NET Core.
