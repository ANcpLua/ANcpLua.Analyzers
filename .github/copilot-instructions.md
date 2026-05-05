# Copilot review instructions

## Reviewer focus

- Review **changes in this PR**, not the whole repo. The diff is the assignment.
- Skip files whose first ~3 lines contain `// Ported from <upstream>`,
  `// Generated`, or `// Auto-generated`. Their contract is the upstream's,
  not ours; surface a one-line note instead of line-level findings.
- Skip files in `node_modules/`, `dist/`, `build/`, `bin/`, `obj/`, generated
  Roslyn artifacts (`*.g.cs`), and lockfiles (`package-lock.json`,
  `pnpm-lock.yaml`, `yarn.lock`).
- Skip vendored fixtures under `**/fixtures/**` and `**/test-data/**` —
  they're deliberately broken or deliberately verbatim.

## Coordinate with other reviewers

- CodeRabbit and Claude Code Review run on the same PR. **Don't repeat
  findings they've already raised** — read the existing review comments
  before posting.
- If CodeRabbit has labeled a finding `false_positive` or the human author
  has marked it resolved, don't re-raise it.

## Style

- Group findings by file, not by severity, when there are >5.
- Don't suggest renames of public exports without a clear caller-side
  benefit. The cost of a rename is paid by every consumer.
- Don't suggest adding tests "for completeness" — only when the changed
  contract is uncovered by existing tests.

## Project conventions to respect

- Node code: ESM, Node ≥20, no external runtime deps unless already
  declared in `package.json`.
- .NET code: nullable enabled, central package management
  (`Directory.Packages.props`), `Version.props` is the single owner of
  versions — never edit `<Version>` lines directly.
- Don't suggest patterns that contradict `CLAUDE.md`, `AGENTS.md`, or the
  repo's `.coderabbit.yaml` `path_instructions`.

## Rate-limit / failure behavior

If you hit a rate limit, **surface the limit and the unblock date in your
review body** rather than the generic "encountered an error" string. The
human author needs the date to plan, not a vague retry hint.
