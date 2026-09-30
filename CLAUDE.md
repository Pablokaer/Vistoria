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
