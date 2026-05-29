# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| latest  | ✅        |

## Reporting a Vulnerability

Please **do not** open a public GitHub issue for security vulnerabilities.

Report vulnerabilities by emailing the maintainer or opening a
[GitHub Security Advisory](../../security/advisories/new) (private disclosure).

Include:

- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if known)

You can expect an acknowledgement within 48 hours and a resolution timeline
within 14 days for confirmed vulnerabilities.

## Scope

This service is a **read-only** OData proxy over the Kimai API. It does not
mutate Kimai data. Security concerns most relevant to this project:

- Authentication/authorisation bypass on the OData endpoints
- Injection vulnerabilities in OData query parameters
- Information disclosure via error responses
- Dependency vulnerabilities in NuGet packages
