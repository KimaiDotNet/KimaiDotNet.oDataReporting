---
applyTo: "docs/features/**,docs/migrations/**"
---

# Feature & Migration Spec Instructions

## Feature specs (`docs/features/`)

Feature specs define **what** will be built and **why**, without prescribing implementation details.

### Required sections

1. **Summary** — one paragraph describing the feature
2. **Goals** — bulleted list of objectives
3. **Non-goals** — explicit exclusions from scope
4. **Requirements** — numbered functional and non-functional requirements
5. **Acceptance Criteria** — testable conditions that must be true for the feature to be considered done
6. **UI / API Contract** (if applicable) — endpoint signatures, request/response shapes, OData query options
7. **Open Questions** — unresolved items (mark resolved when answered)

## Migration specs (`docs/migrations/`)

Migration specs document **breaking changes** or **data/schema migrations**.

### Required sections

1. **Summary** — what is changing and why
2. **Impact** — who and what is affected (clients, dashboards, configuration)
3. **Migration Steps** — numbered, ordered steps to migrate
4. **Rollback Plan** — how to revert if the migration fails
5. **Verification** — how to confirm the migration succeeded

## Guidelines

- Write requirements as "The system shall…" or "Users can…"
- Acceptance criteria must be independently verifiable
- For OData service changes: include the affected entity sets and any EDM model changes
