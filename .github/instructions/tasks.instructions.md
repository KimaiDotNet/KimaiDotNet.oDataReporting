---
applyTo: "**"
---

# Task Decomposition Instructions

## When to decompose

Decompose a feature or story into tasks when the work spans more than one day or more than one logical concern (API, model, tests, docs).

## Task format

Each task must have:

- **Title** — verb-first, specific (e.g., "Add `UserController` OData endpoint for team membership")
- **Description** — 2–4 sentences describing what to do and why
- **Acceptance Criteria** — at least one testable condition
- **Dependencies** — other tasks that must complete first (if any)
- **Size estimate** — S / M / L / XL

## Sizing guide

| Size | Description |
|------|-------------|
| S    | < 2 hours; single, well-understood change |
| M    | 2–4 hours; a focused but complete unit of work |
| L    | 4–8 hours; multiple related changes across files |
| XL   | > 8 hours; must be broken down further |

## Guidelines for this project

- Controller tasks must include the corresponding xUnit test task
- Configuration changes must include an update to `appsettings.json` and documentation
- Any new OData entity set must include an EDM model update task and a dashboard verification task
