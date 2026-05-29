# Security Policy

## Supported versions

| Version | Supported |
|---------|-----------|
| Latest (`main`) | ✅ |
| Older releases | ❌ |

## Reporting a vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

To report a security vulnerability, please open a [GitHub Security Advisory](https://github.com/kimaidotnet/KimaiDotNet.oDataReporting/security/advisories/new) in this repository. This keeps the disclosure confidential while we investigate and prepare a fix.

Please include:

- A description of the vulnerability and its potential impact
- Steps to reproduce (proof of concept if possible)
- The version(s) affected
- Any suggested mitigations

We aim to acknowledge reports within **48 hours** and provide an initial assessment within **7 days**.

## Security considerations

This service proxies Kimai time-tracking data as a read-only OData feed. Key security notes:

- **Read-only**: the service performs no write operations against the Kimai API
- **Authentication**: Kimai API credentials must be supplied via environment variables or secrets management — never committed to source control
- **Input sanitisation**: all data received from the Kimai API is sanitised before being surfaced through the OData endpoint
- **Chaos engineering**: chaos injection is disabled by default; enable only in controlled environments

## Scope

In-scope for this security policy:

- `src/KimaiDotNet.Reporting.ODataService/` — the OData service
- Docker image `markzither/kimai.net_odatareporting`

Out of scope:

- The upstream Kimai application (report vulnerabilities to the [Kimai project](https://github.com/kimai/kimai))
- Dashboard files under `src/Dashboards/` (Power BI, SiSense, Tableau)
