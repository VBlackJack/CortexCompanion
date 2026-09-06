# Changelog

## [Unreleased]

### Added

- Home overview with observed indexing and collection times, next scheduled run,
  current activity, and a contextual next action.
- Four-step getting-started guide that uses saved configuration and observed
  indexing outcomes, with optional Confluence setup.
- Retained manual and scheduled operation history with explicit partial,
  cancelled, unconfirmed and unreadable outcomes, exact available counters,
  error samples and recovery navigation.
- On-demand stable release checks showing both installed versions and the
  available version, with navigation to the official combined installer.
- Search preview and explicit excerpt/reference copying; advanced filters
  start collapsed and leave more space for results.

### Fixed

- Successful local-only indexing now exposes its observed completion timestamp
  without claiming that a Confluence generation has been indexed.

## [2026.0906.00] - 2026-09-06

### Added

- Search destination with section/source filters, bounded excerpts and explicit
  source opening through Cortex's versioned JSON contract. This requires a Cortex
  build that provides `search --json`.
- Separate publication and successful indexing observations, using the current
  generation pointer and bounded durable Companion run history.
- Python/C# search contract proof and a manual `release-pair` workflow accepting
  two immutable commit SHAs.
- Recovery checks for an exited worker, invalid search responses and stale result
  clearing, plus keyboard traversal and WPF rasterization checks.

### Fixed

- Keep unsaved knowledge-base and scheduling edits across refresh and navigation.
- Gate desktop search on Cortex 2026.0906.00 or newer, independently of other features.
- Support search cancellation, retain executed criteria and discard obsolete results.
- Explain unavailable source opening, expose indexing freshness and link recovery actions.
- Direct first-time Confluence authentication to the graphical configuration screen.

- Continue draining process output after log persistence fails, preserving the
  child exit code and reporting capture failure separately.
- Canonical empty page selections now match Cortex's TOML renderer, including
  schema-v3 coverage.
- Search failures always replace the running status with an actionable error.
- Long navigation labels wrap, and search controls use the shared theme.

### Documentation

- English and French guides describe compatibility, freshness evidence limits,
  local tests, exact paired-commit validation and remaining manual accessibility
  and physical DPI checks.
