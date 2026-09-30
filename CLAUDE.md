# Working rules for this repository

## After every code change (always, without being asked)

1. **Update the documentation** so it matches the code:
   - `README.md` — features, run instructions, configuration, demo users, "How to test the flows", API overview.
   - `docs/CHANGELOG.md` — add an entry (newest first, dated) describing the change.
   - `docs/architecture/` — add or update an ADR when a design decision changes, and list it in `docs/architecture/README.md`.
   - `docs/IMPLEMENTATION_REPORT.md` — keep the feature summary and test counts current.
2. **Verify**: frontend `npx tsc --noEmit` and `npx eslint src` (in `frontend/`), backend `dotnet test` (in `backend/`); run the UI flow (`e2e/`, `node ui-flow.mjs`) when user-facing flows change — with `AI_PROVIDER=Mock docker compose up --build -d` so tests do not spend OpenAI credits, then restore with `docker compose up -d`.
3. **Commit and push** to `origin main` (https://github.com/Pablokaer/Vistoria.git): one commit per logical change, code and docs together.

## Never commit

- `.env` (holds the real `OPENAI_API_KEY`); it is gitignored — keep it that way and check staged files for `sk-` keys before committing.

## Code style

- Functions: 4-20 lines. Split if longer.
- Files: under 500 lines. Split by responsibility.
- One thing per function, one responsibility per module (SRP).
- Names: specific and unique. Avoid `data`, `handler`, `Manager`.
  Prefer names that return <5 grep hits in the codebase.
- Types: explicit. No `any`, no `Dict`, no untyped functions.
- No code duplication. Extract shared logic into a function/module.
- Early returns over nested ifs. Max 2 levels of indentation.
- Exception messages must include the offending value and expected shape.

## Comments

- Keep your own comments. Don't strip them on refactor — they carry
  intent and provenance.
- Write WHY, not WHAT. Skip `// increment counter` above `i++`.
- Docstrings on public functions: intent + one usage example.
- Reference issue numbers / commit SHAs when a line exists because
  of a specific bug or upstream constraint.

## Tests

- Tests run with a single command: `dotnet test` (in `backend/`). The full UI flow runs with `node ui-flow.mjs` (in `e2e/`, against a running stack).
- Every new function gets a test. Bug fixes get a regression test.
- Mock external I/O (API, DB, filesystem) with named fake classes,
  not inline stubs.
- Tests must be F.I.R.S.T: fast, independent, repeatable,
  self-validating, timely.

## Dependencies

- Inject dependencies through constructor/parameter, not global/import.
- Wrap third-party libs behind a thin interface owned by this project.

## Structure

- Follow the framework's convention (Rails, Django, Next.js, etc.).
- Prefer small focused modules over god files.
- Predictable paths: controller/model/view, src/lib/test, etc.

## Formatting

- Use the language default formatter (`cargo fmt`, `gofmt`, `prettier`,
  `black`, `rubocop -A`). Don't discuss style beyond that.

## Logging

- Structured JSON when logging for debugging / observability.
- Plain text only for user-facing CLI output.
