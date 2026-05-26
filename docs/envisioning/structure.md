# Structure (Cache)

> **Source of truth**: GitHub Issues board. Check the board for current state.
> **Platform**: GitHub Issues — `KimaiDotNet/KimaiDotNet.oDataReporting`
> **Last synced**: 2026-05-26

## Hierarchy

| Issue | Type | Title | Parent | Priority | Size | Labels |
|-------|------|-------|--------|----------|------|--------|
| [#4](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/4) | Epic | .NET 10 LTS Upgrade (+ .NET 11 Preview 4 validation) | — | — | — | epic, enhancement |
| [#15](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/15) | Feature | Upgrade ODataService project to net10.0 | #4 | P1 | M | enhancement, needs-investigation |
| [#22](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/22) | Feature | Create xUnit test project (baseline tests) | #4 | P1 | M | enhancement |
| [#13](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/13) | Feature | Update Dockerfile base image to .NET 10 | #4 | P1 | S | enhancement |
| [#6](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/6) | Feature | Validate .NET 11 Preview 4 compatibility | #4 | P2 | S | enhancement |
| [#3](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/3) | Epic | MCP Server (HTTP + stdio) | — | — | — | epic, enhancement |
| [#19](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/19) | Feature | Implement MCP server core with HTTP transport | #3 | P1 | L | enhancement, needs-investigation |
| [#8](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/8) | Feature | Add stdio transport to MCP server | #3 | P1 | M | enhancement |
| [#10](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/10) | Feature | MCP tool — query time entries by user/team | #3 | P1 | M | enhancement |
| [#11](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/11) | Feature | MCP tool — create time entry (write via MCP) | #3 | P2 | M | enhancement |
| [#2](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/2) | Epic | Security Hardening — Kimai API Key | — | — | — | epic, security |
| [#18](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/18) | Feature | Implement secure API key configuration (env/user-secrets) | #2 | P1 | M | security, enhancement |
| [#7](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/7) | Feature | Add log-scrubbing to prevent API key leakage | #2 | P1 | S | security |
| [#14](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/14) | Feature | Add secret hygiene automated test | #2 | P1 | S | security |
| [#1](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/1) | Epic | NuGet Global Tool Packaging + Docker Update | — | — | — | epic, enhancement |
| [#12](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/12) | Feature | Configure project as dotnet global tool | #1 | P1 | M | enhancement |
| [#9](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/9) | Feature | Update Docker image and CI publishing pipeline | #1 | P1 | M | enhancement |
| [#16](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/16) | Feature | Publish package to NuGet.org | #1 | P2 | S | enhancement |
| [#5](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/5) | Epic | Azure DevOps Integration (OAuth, calendar/sprint) | — | — | — | epic, enhancement |
| [#20](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/20) | Feature | OAuth 2.0 flow for Azure DevOps | #5 | P1 | L | enhancement, open-question |
| [#17](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/17) | Feature | Sprint/calendar data retrieval via ADO API | #5 | P1 | M | enhancement |
| [#21](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/21) | Feature | Expose ADO sprint data via OData or MCP tool | #5 | P2 | M | enhancement |

## Dependencies

```
#15 (net10.0 upgrade)  ──────────────────────────────┐
#22 (xUnit test project)                              │
  └─► #13 (Dockerfile update)                        │
  └─► #6  (net11 validation)                         │
                                                      ▼
#7  (log-scrubbing)                           #3  (MCP Server Epic)
  └─► #14 (secret hygiene test)              ├─► #19 (HTTP transport) [needs-investigation]
                                             │     └─► #8  (stdio transport)
#18 (secure API key config) ─► depends #4   │           └─► #10 (query tool)
                                             │           └─► #11 (create tool)
#4  (Epic .NET 10) ──┐
#2  (Epic Security)──┼──► #12 (global tool)
                     └──► #9  (CI pipeline)
                           └─► #16 (NuGet publish)

#4 + #2 ──► #20 (ADO OAuth) [open-question]
              └─► #17 (sprint retrieval)
                    └─► #21 (expose via OData/MCP)
```

## Risk Register

| Issue | Risk | Type |
|-------|------|------|
| [#15](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/15) | Polly/Simmy .NET 10 package compatibility | needs-investigation |
| [#19](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/19) | MCP .NET SDK maturity (pre-release) | needs-investigation |
| [#20](https://github.com/KimaiDotNet/KimaiDotNet.oDataReporting/issues/20) | ADO OAuth tenant type unresolved (MSA vs AAD/Entra) | open-question |
