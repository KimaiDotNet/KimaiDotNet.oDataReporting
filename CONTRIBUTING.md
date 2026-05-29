# Contributing to KimaiDotNet.Reporting

Thank you for your interest in contributing!

## Getting started

### Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- Docker (for building and testing the container image)
- A running [Kimai](https://www.kimai.org/) instance or access to a test environment

### Build and run locally

```bash
# Restore dependencies
dotnet restore KimaiDotNet.Reporting.sln

# Build
dotnet build KimaiDotNet.Reporting.sln

# Run the OData service
cd src/KimaiDotNet.Reporting.ODataService
dotnet run
```

### Run tests

```bash
dotnet test KimaiDotNet.Reporting.sln
```

### Build the Docker image

```bash
docker build -t markzither/kimai.net_odatareporting \
  -f src/KimaiDotNet.Reporting.ODataService/Dockerfile .
```

## Development workflow

1. Fork the repository and create a feature branch from `main`:
   ```
   git checkout -b feature/<short-description>
   ```
2. Make your changes following the [coding guidelines](.github/docs/coding-guidelines.md)
3. Add or update tests as appropriate
4. Run the full test suite to ensure nothing is broken
5. Commit using [Conventional Commits](https://www.conventionalcommits.org/):
   ```
   feat: add activity entity set filter support
   fix: correct null reference in TeamMembershipController
   ```
6. Open a pull request against `main`

## Pull request guidelines

- Keep PRs focused: one feature or fix per PR
- Include a clear description of what changed and why
- Reference any related issue numbers
- Ensure all CI checks pass before requesting review
- Update documentation if your change affects public behaviour or configuration

## Code style

Follow the guidelines in [.github/docs/coding-guidelines.md](.github/docs/coding-guidelines.md).

A `.editorconfig` file is included at the repository root — ensure your editor respects it.

## Reporting issues

Use [GitHub Issues](https://github.com/kimaidotnet/KimaiDotNet.oDataReporting/issues) for bug reports and feature requests. For security vulnerabilities, see [SECURITY.md](SECURITY.md).

## Code of conduct

This project follows the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold it.
