# Coding Guidelines — KimaiDotNet.Reporting

## Language and framework

- **Language**: C# 11 (.NET 7)
- **Framework**: ASP.NET Core + Microsoft.AspNetCore.OData
- **Test framework**: xUnit

## General principles

- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Prefer explicit types over `var` when the type is not obvious from the right-hand side
- Keep methods short: if a method is more than 30 lines, consider splitting it
- Avoid magic strings and numbers; use named constants or configuration values

## Naming

| Construct | Convention | Example |
|-----------|-----------|---------|
| Class | PascalCase | `TimesheetController` |
| Interface | `I` + PascalCase | `IKimaiClient` |
| Method | PascalCase | `GetTimesheets` |
| Private field | `_camelCase` | `_kimaiOptions` |
| Local variable | camelCase | `timesheetEntry` |
| Constant | PascalCase | `DefaultPageSize` |
| Configuration key | PascalCase section | `KimaiOptions:BaseUrl` |

## Controllers

- Keep controller actions thin: delegate all business logic to services or the Kimai API client
- Return `IActionResult` or `ActionResult<T>`; never expose domain exceptions directly
- All OData controllers must register their entity set in `EdmModelBuilder.cs`
- The service is **read-only**: `GET` only — no `POST`, `PUT`, `PATCH`, or `DELETE`

## Configuration

- Add new configuration classes under `Configuration/`
- Bind configuration in `Program.cs` using `services.Configure<T>()`
- Validate required configuration at startup using `IValidateOptions<T>`
- Never hard-code secrets; use environment variables or user secrets in development

## Chaos engineering

- Chaos policies live in `Extensions/` and `Configuration/`
- All chaos behaviour is opt-in via `GeneralChaosOptions` and `OperationChaosOptions`
- Default injection rates must be `0.0` (disabled) in production configuration

## Error handling

- Use `ILogger<T>` for all logging; inject via constructor
- Log with structured properties, not string interpolation: `_logger.LogWarning("Fetching {Count} timesheets", count)`
- Use `EventIds.cs` for event ID constants
- Do not swallow exceptions silently; log and rethrow or return appropriate HTTP status

## Security

- Sanitise all inputs received from the Kimai API before surfacing through OData
- Do not log sensitive data (tokens, passwords, personal data)
- Follow OWASP Top 10 guidance
- Authentication tokens must never be stored in source code or committed to the repository

## Testing

- Test projects mirror the source namespace: `KimaiDotNet.Reporting.ODataService.Tests`
- Test class names: `<ClassUnderTest>Tests`
- Test method names: `<MethodName>_<Scenario>_<ExpectedOutcome>`
- Use xUnit `[Fact]` for single-case tests, `[Theory]` + `[InlineData]` for parameterised cases
- Mock external dependencies (Kimai API, configuration) using `NSubstitute` or `Moq`

## Docker

- Dockerfile lives at `src/KimaiDotNet.Reporting.ODataService/Dockerfile`
- Image: `markzither/kimai.net_odatareporting`
- Do not include development-only dependencies in the production image

## Commit style

Use Conventional Commits:

```
feat: add activity entity set to EDM model
fix: correct null reference in TeamMembershipController
chore: update NuGet packages
docs: document chaos configuration options
refactor: extract Kimai client retry policy to extension
test: add unit tests for ExportController
```
