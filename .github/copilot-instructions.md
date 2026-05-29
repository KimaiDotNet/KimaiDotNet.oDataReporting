# Copilot Instructions — KimaiDotNet.Reporting OData Service

## Project Overview

This repository contains a .NET 7 OData service (`KimaiDotNet.Reporting.ODataService`) that exposes [Kimai](https://www.kimai.org/) time-tracking data as an OData feed. Dashboard clients (Power BI, SiSense, Tableau) consume this service.

- **Solution**: `KimaiDotNet.Reporting.sln`
- **Service path**: `src/KimaiDotNet.Reporting.ODataService/`
- **Docker image**: `markzither/kimai.net_odatareporting`
- **Target framework**: .NET 7

## Architecture

- ASP.NET Core + Microsoft.AspNetCore.OData
- Controllers: `Activity`, `Customer`, `Export`, `Project`, `Team`, `TeamMembership`, `Timesheet`, `User`
- Chaos engineering via Polly + Simmy (`Extensions/`, `Configuration/`)
- EDM model defined in `Models/EdmModelBuilder.cs`
- Configuration: `KimaiOptions`, `oDataServiceOptions`, `GeneralChaosOptions`, `OperationChaosOptions`

## Coding Guidelines

Follow the guidelines in [.github/docs/coding-guidelines.md](.github/docs/coding-guidelines.md).

## Documentation

- Feature specs → `docs/features/`
- Migration specs → `docs/migrations/`
- Architecture decisions → `docs/architecture/decisions/`
- Envisioning documents → `docs/envisioning/`
- Templates: use files named `TEMPLATE.md` in each folder

## Agent Behaviour

- **Language**: English for all responses, specs, ADRs, and work items
- **Commit style**: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`, `test:`)
- **Branch strategy**: feature branches off `main`, name pattern `feature/<short-description>`
- **Tests**: xUnit; test project should mirror the source namespace
- **Security**: Follow OWASP Top 10; sanitise all inputs from Kimai API responses before surfacing through OData

## Key Constraints

- The OData service is read-only; no mutations to Kimai data are performed here
- Chaos options are opt-in via configuration; default to disabled in production
- Keep controller actions thin — delegate to services or the Kimai API client
