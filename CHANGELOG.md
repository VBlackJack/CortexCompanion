# Changelog

## [Unreleased]

### Removed

- Drop the search-only version floor and the upgrade message it produced. The accepted
  floor is the CLI this build ships with, which already carries the search contract, so a
  second boundary below it could no longer refuse anything: a handshake that passed had
  cleared a later version. Search now follows the startup handshake alone, and the
  documentation says so instead of naming a version.

## [2026.0907.03] - 2026-09-07

### Added

- Prove every Cortex command line the desktop builds against the Cortex parser that runs
  it. The document proofs checked what Cortex answers, and nothing checked what the desktop
  asks: a renamed subcommand, a parent option moved after its subcommand or a dropped
  required flag stayed green on both sides and broke the desktop at run time. The probe now
  captures the sixteen lines from the code that builds them, and the proof parses each with
  Cortex, stopping before anything runs, then checks that every value landed in the slot the
  desktop meant it for. Verified to fail on a renamed subcommand, a renamed parent option, a
  renamed required flag, a renamed search option and a version flag that prints something
  other than the version. Both interoperability workflows and the paired release gate run it.

### Changed

- Refuse a Cortex older than 2026.0907.03, the one this build ships with. Nothing in this
  pair changes a contract, but the floor follows the shipped version by rule, and a test
  holds it there.

## [2026.0907.02] - 2026-09-07

### Added

- Say in the scope window when the page is already tracked. Whether it can be added depends
  on the scope chosen, so the answer cannot be settled before the question; showing it stops
  the user from choosing carefully and only then being refused.

### Changed

- Refuse a Cortex older than the one this build ships with. The floor had been left at
  2026.0808.00 while the product reached 2026.0907.01, so a mismatched pair reached the user
  as an unexplained parse failure instead of a version refusal. A test now ties the floor to
  the shipped version, and the handshake tests follow the constant rather than frozen
  literals, which is what had made raising it break seven unrelated tests.

### Fixed

- Let the page tree finish loading. Reading a whole space takes about two minutes on a
  large one, measured at 112 seconds over thirty requests for 5918 pages: past the default
  of thirty seconds, and inside the highest offered value by eight, which is no margin at
  all for a slower server. That read now has its own floor of five minutes,
  while every other command keeps the configured timeout; a timeout longer than the floor
  is still honoured.
- Say how far the page tree got when it does run out of time. Cortex reports its progress
  on the diagnostic stream, so the message names the pages read and the estimate instead of
  repeating the generic timeout advice.

## [2026.0907.01] - 2026-09-07

### Added

- Ship the interface in English as well as French. UiStrings.resx now holds English and is
  the neutral resource, the French set moves to UiStrings.fr.resx as a satellite, and the
  project declares NeutralResourcesLanguage so a Windows language Companion does not ship
  reads English rather than failing to resolve a string. Until now a single French resource
  set was the neutral one, so every machine read French whatever its language.
- Add an interface language setting, kept in settings.json and offered in Settings. It
  follows the Windows language by default and each language is named in its own language.
  The choice applies at the next start: several localized members are static formats parsed
  once when their type is first touched, so switching in place would leave part of the
  interface in the previous language. UiCultureBootstrap therefore reads the preference
  straight from the settings file at the top of Main, before anything reaches those formats,
  and treats a missing, locked, truncated or hand-edited file as no preference at all.
- Guard the two resource sets against drift: they must answer for the same keys with the
  same placeholders, and the typography guard now scans every UiStrings resource file rather
  than one named path.

## [2026.0907.00] - 2026-09-07

- Restore the contrast of the scope selection window. Its three options declared
  neither a style nor a foreground, so they fell through to the built-in control
  style and were painted in the system text colour over the dark background. A new
  theme guard now requires every RadioButton and CheckBox in a view to declare one.
- Stop presenting a CLI timeout as something else. Loading a page tree reported a
  tree error and blamed the network connection; the timeout advice promised an
  increase that reverts to the default for any value outside the offered choices,
  and now names the highest selectable delay instead.
- Carry the timeout fact on the CLI operation error, so the four places that present
  it no longer pass a hardcoded false and can reach the message that names the cause.
- Keep machine progress records out of the sentence read aloud in the live region.

## [2026.0906.02] - 2026-09-06

- Put My sources first, add source/title filtering and searchable remote page trees.
- Show conservative publication/index readiness and effective before/after document
  differences, with explicit unavailable measurements.
- Add save-and-update, save-for-later, CAS-protected session removal undo and
  persistent recovery actions, including expiry validation during reconnection.

- Visible My sources cards with a prefilled selection editor, explicit common
  scope, confirmed page/space removal and browser links to original content.
- Best-effort remaining subtree coverage check before individual root removal.
- Collect/index follow-up for saved changes and a useful route for already covered pages.
- Preserve the distinction between a missing allowlist and an explicitly emptied
  allowlist, including last-source removal. Requires the paired Cortex change.

## [2026.0906.01] - 2026-09-06

### Added

- Single page-or-space link flow with inline credential recovery, measured scope
  confirmation, isolated preview settings and one atomic source write. Cancellation
  and authentication errors leave configured sources unchanged.
- Guided collection followed by indexing only after an observed successful run,
  with separate progress and search navigation.

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
