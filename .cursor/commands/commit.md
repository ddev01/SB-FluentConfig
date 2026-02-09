# Git Commit Assistant

Analyze changes, decide commit structure, and execute git commits.

## Workflow

1. **Gather context** — Run `git status --short` and `git diff` (or `git diff --staged` if staged). Use conversation history and any pasted diffs to understand what changed.
2. **Analyze** — Determine type, scope, and whether changes are logically grouped or unrelated.
3. **Split decision** — If unrelated changes (e.g. API + UI, refactor + fix), split into multiple commits. Otherwise, single commit.
4. **Commit** — Stage and commit:
   - Single: `git add -A && git commit -m "message"`
   - Multiple: stage and commit each group separately (e.g. `git add path1 path2 && git commit -m "..."`, then `git add path3 && git commit -m "..."`)

## Commit Format

`type(scope): subject`
- Types: feat, fix, refactor, perf, style, test, docs, chore, build, ci, etc.
- Subject: lowercase, imperative, max 72 chars, no period
- Breaking: add `!` after type (e.g. `feat!: breaking change`)

## Rules

- Always commit automatically unless user says "just show message"
- Keep messages concise — focus on WHAT changed and WHY
- When splitting: explain the split briefly, then do all commits
