---
applyTo: "docs/**,README.md,.github/**/*.md"
---

# Documentation Style Instructions

## General principles

- Write in plain, direct English
- Prefer active voice over passive voice
- Use sentence case for headings (not Title Case), except for proper nouns
- Keep paragraphs short: 3–5 sentences maximum
- Use bullet lists for 3 or more parallel items; avoid single-item lists

## Markdown formatting

- Use ATX headings (`#`, `##`, `###`) — never Setext underline style
- Leave one blank line before and after headings
- Leave one blank line before and after code blocks
- Use fenced code blocks with a language hint (` ```csharp `, ` ```json `, ` ```bash `)
- Ordered lists for sequential steps; unordered for non-sequential items
- Use `**bold**` for UI labels and key terms on first use; avoid overuse
- Use `_italic_` sparingly — only for genuine emphasis
- Do not use bare URLs; always use `[descriptive text](url)`

## File naming

- Lowercase with hyphens: `feature-name.md`, `ADR-0001-use-odata.md`
- Templates always named `TEMPLATE.md` in their respective folders

## Tables

- Always include a header row
- Align columns consistently
- Keep table content concise — link out to longer explanations rather than embedding them

## Code references

- Wrap inline code, file paths, and configuration keys in backticks: `KimaiOptions`, `appsettings.json`
- For longer code samples, use a fenced code block with a language specifier

## Linting

This repository uses `markdownlint`. See `.markdownlint.yaml` at the repo root for the active rule set.
