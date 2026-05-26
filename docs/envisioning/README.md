# Envisioning: KimaiDotNet.Reporting — Multi-Protocol Analytics & Agent Platform

**Status**: Draft
**Author**: Mark
**Created**: 2026-05-26
**Last updated**: 2026-05-26
**Version**: 1.1
**Stakeholders**: Mark (owner/maintainer), Kimai self-hosted operators

---

## Vision

KimaiDotNet.Reporting evolves from a read-only OData bridge into a multi-protocol analytics and agent-accessible platform for Kimai time-tracking data. When complete, a developer or team can install a single NuGet tool, point it at their Kimai instance, and immediately gain rich BI connectivity (Power BI, SiSense, Tableau), AI-agent access via MCP, and Azure DevOps calendar context — all running on a current, supported .NET runtime with production-grade secret management.

## Problem statement

Kimai's built-in reporting UI does not meet the analytical needs of teams that want cross-project aggregation, custom KPIs, or integration with existing BI tooling. The existing OData service partially solves this but has accumulated technical debt:

- Targets .NET 7, which reached end-of-support in May 2024, blocking security patches and modern APIs.
- Exposes no interface for AI agents or conversational tooling (no MCP server).
- Has no integrations beyond the Kimai API (no calendar context from Azure DevOps).
- API key handling has not been formally hardened against secret exposure.
- Distribution requires Docker only; no lower-friction install option exists.

## Scope

### In scope

- Upgrade runtime to .NET 10 LTS (primary target) with .NET 11 Preview 4 compatibility validated
- Add an MCP server supporting both HTTP and stdio transports (implemented in .NET)
- MCP tools: query hours entered by user/team, enter time entries on behalf of users
- Limited agent interactivity via MCP sampling where the protocol supports it
- Azure DevOps integration: read calendar/sprint data via OAuth to provide scheduling context
- Secure Kimai API key handling (environment variables, secrets manager, or .NET user-secrets)
- OAuth 2.0 flow for Azure DevOps authentication
- Publish service as a .NET NuGet global tool (alongside existing Docker image)
- Maintain existing OData endpoints and read-only contract

### Out of scope

- Writing back to Kimai via OData (OData layer remains read-only)
- Support for Azure DevOps work-item mutation
- Non-.NET MCP server implementations
- Kimai UI modifications
- Support for Kimai Cloud (self-hosted only)

## Success criteria

| Criterion | Measurement | Target |
|-----------|-------------|--------|
| Runtime currency | .NET target framework | net10.0 (LTS); net11.0-preview validated |
| MCP connectivity | MCP server responds to `tools/list` over HTTP and stdio | Both transports pass integration test |
| Hour query via agent | Agent can retrieve time entries for a named user | Returns correct entries in manual test |
| Hour entry via agent | Agent can create a time entry via MCP tool | Entry visible in Kimai UI after invocation |
| NuGet tool install | `dotnet tool install -g KimaiDotNet.Reporting` runs the server | Tool starts and serves OData + MCP endpoints |
| Azure DevOps calendar | Sprint/calendar data returned by a dedicated OData or MCP tool | Data matches ADO sprint dates |
| Secret hygiene | Kimai API key never logged or returned in responses | Verified by log-scraping test |
| OAuth for ADO | Azure DevOps calls use OAuth token, not PAT | Auth flow tested with a real AAD app registration |

## Stakeholders

| Name / Role | Interest | Involvement |
|-------------|----------|-------------|
| Mark, Owner/Maintainer | Full platform vision and delivery | Approver |
| Kimai self-hosted operators | Easy install, reliable BI connectivity | Consulted (community feedback) |
| BI consumers (Power BI, Tableau users) | Stable OData feed | Informed |

## Customers

**Direct customer**: The developer or team operating a self-hosted Kimai instance who wants analytics and agent access beyond the built-in UI.

**End customer**: Kimai users (project managers, consultants, developers) who track time and need that data surfaced in BI dashboards or via AI assistants.

## Pain points

### Business

| # | Pain point | Impact |
|---|-----------|--------|
| B1 | Kimai's built-in reports lack cross-project aggregation and custom KPI support | Teams cannot produce board-level analytics without exporting CSV manually |
| B2 | No conversational or agent-based access to time data | AI workflows (Copilot, Claude) cannot query or log time without a custom integration |
| B3 | No calendar/sprint context alongside time data | Effort cannot be correlated to sprint goals without manual join |

### Technical

| # | Pain point | Category |
|---|-----------|---------|
| T1 | .NET 7 end-of-support; target .NET 10 LTS + .NET 11 Preview 4 | Maintainability / Security |
| T2 | No MCP server; no agent-accessible interface | Agility / Integration |
| T3 | API key passed in configuration without formal secret hygiene | Security |
| T4 | Azure DevOps calendar data not integrated; no OAuth flow exists | Integration |
| T5 | Distribution is Docker-only; high friction for local/dev use | Agility |

## Strategic goals

**Business goal**: Make Kimai time data first-class input for BI tools and AI agents, enabling teams to answer resourcing and delivery questions without leaving their preferred tooling.

**Technical goal**: Modernise to .NET 10 LTS (with .NET 11 Preview 4 validated), introduce a standards-based MCP interface, harden secret handling, and lower distribution friction to a single `dotnet tool install` command.

## KPIs

1. Zero known CVEs in transitive dependencies at release (Dependabot / `dotnet list package --vulnerable`)
2. MCP server listed and functional in at least one AI agent client (e.g., VS Code Copilot, Claude Desktop)
3. End-to-end time-entry round-trip (create via MCP → confirm in Kimai UI) succeeds in < 5 seconds
4. NuGet package published to nuget.org with ≥ 1 verified install by a non-owner

## Open questions

| # | Question | Owner | Status |
|---|----------|-------|--------|
| 1 | Which .NET secrets provider to use for Kimai API key in production? (Azure Key Vault, environment variable, .NET user-secrets) | Mark | Open |
| 2 | Does MCP sampling (agent interactivity / elicitation) meet our UX needs, or is a prompt-loop sufficient? | Mark | Open |
| 3 | Will the NuGet tool host both OData and MCP on the same port, or separate ports? | Mark | Open |
| 4 | Is the ADO integration read-only (sprints/calendar) or should it also allow linking time entries to work items? | Mark | Open |
| 5 | Target AAD tenant for OAuth: personal MSA, organisational, or multi-tenant? | Mark | Open |

## Proposed workstreams (epic order)

| # | Workstream | Notes |
|---|-----------|-------|
| 1 | .NET 10 LTS upgrade (+ .NET 11 Preview 4 validation) | Foundation for all other work |
| 2 | MCP server (HTTP + stdio) | Core new capability |
| 3 | Security hardening — Kimai API key | Unblock safe distribution |
| 4 | NuGet global tool packaging + Docker update | Distribution |
| 5 | Azure DevOps integration (OAuth, calendar/sprint) | Builds on stable, secure foundation |

## Related artifacts

- Feature spec: `docs/features/` (to be created)
- ADR: `docs/architecture/decisions/` (to be created)
- Envisioning template: [docs/envisioning/TEMPLATE.md](TEMPLATE.md)
