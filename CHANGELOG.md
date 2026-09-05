# Changelog

## Unreleased

These changes are available in source; no new release version is assigned here.

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
