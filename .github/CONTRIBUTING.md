# Contributing to KimaiDotNet.Reporting

Thank you for considering a contribution!

## Getting Started

1. Fork the repository and create a feature branch from `main`:

   ```bash
   git checkout -b feature/<short-description>
   ```

2. Ensure you have the following installed:
   - [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
   - [Docker](https://www.docker.com/) (optional, for container testing)

3. Build and run locally:

   ```bash
   dotnet build KimaiDotNet.Reporting.sln
   dotnet run --project src/KimaiDotNet.Reporting.ODataService
   ```

## Development Guidelines

- Follow the coding guidelines in [.github/docs/coding-guidelines.md](docs/coding-guidelines.md)
- Keep controller actions thin — delegate to services or the Kimai API client
- The OData service is **read-only**; do not add mutations to Kimai data
- Chaos options (`GeneralChaosOptions`, `OperationChaosOptions`) must default to disabled

## Testing

- Use xUnit for all tests
- Test project namespaces should mirror the source namespace
- Run tests before opening a PR:

  ```bash
  dotnet test KimaiDotNet.Reporting.sln
  ```

## Commit Messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add team membership endpoint
fix: correct OData EDM model for Timesheet
docs: update README with Docker run instructions
```

## Pull Requests

- Open a PR against `main`
- Fill in the PR template completely
- Link any related issues
- Ensure CI passes before requesting review

## Code of Conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md).
By participating you agree to abide by its terms.
