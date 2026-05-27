# Feature Spec: [Feature Title]

**Status**: Draft | Review | Approved  
**Author**: [Name]  
**Created**: YYYY-MM-DD  
**Last updated**: YYYY-MM-DD  
**Related ADRs**: [ADR-XXXX](../architecture/decisions/ADR-XXXX-title.md)

---

## Summary

[One paragraph describing what this feature is and why it is being built.]

## Goals

- [Goal 1]
- [Goal 2]
- [Goal 3]

## Non-goals

- [Explicitly excluded item 1]
- [Explicitly excluded item 2]

## Requirements

### Functional

1. The system shall [requirement 1].
2. The system shall [requirement 2].
3. Users can [requirement 3].

### Non-functional

1. [Performance, scalability, security, or availability requirement.]
2. [Another non-functional requirement.]

## API / OData contract

> Include this section only if the feature adds or changes OData entity sets or endpoints.

### Affected entity sets

| Entity set | Change | Notes |
|------------|--------|-------|
| `EntitySets` | Added / Modified / Removed | [description] |

### EDM model changes

```csharp
// Describe the changes to EdmModelBuilder.cs
```

## Acceptance criteria

- [ ] [Criterion 1: testable condition]
- [ ] [Criterion 2: testable condition]
- [ ] [Criterion 3: testable condition]

## Open questions

| # | Question | Owner | Status |
|---|----------|-------|--------|
| 1 | [Question text] | [Name] | Open / Resolved: [answer] |

## Dependencies

- [Dependency on another feature, service, or external system]
