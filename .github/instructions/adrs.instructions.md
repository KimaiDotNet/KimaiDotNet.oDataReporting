---
applyTo: "docs/architecture/decisions/**"
---

# ADR Writing Instructions

## Format

Every ADR in `docs/architecture/decisions/` must follow the template at `ADR-TEMPLATE.md`.

## Numbering

ADRs are numbered sequentially: `ADR-0001-<short-title>.md`, `ADR-0002-<short-title>.md`, etc.

## Status values

`Proposed` → `Accepted` → `Deprecated` / `Superseded by ADR-XXXX`

## Required sections

1. **Title** — imperative, descriptive (e.g., "Use OData v4 for feed exposure")
2. **Status** — current status + date
3. **Context** — why a decision is needed; constraints and forces
4. **Decision** — what was decided, stated clearly
5. **Consequences** — trade-offs, positive and negative outcomes
6. **Alternatives considered** — other options evaluated and why they were rejected

## Quality bar

- Decision must be falsifiable: it states what will be done, not what might be done
- Consequences must include at least one trade-off
- Alternatives section must list at least one rejected option with rationale
