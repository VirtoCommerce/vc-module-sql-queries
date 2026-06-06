# Claude Code guidance — vc-module-sql-queries

This repo ships two project-scoped skills under `.claude/skills/` for automating the
SQL Queries module's reporting via its REST API. Both are grounded in the module source
(controller routes, model field names, and the `sql-queries:*` permissions).

## Which skill to use

Pick by **intent**, not by who the user is:

- **vc-sql-queries-admin** — *authoring & lifecycle.* Use when the request is to **create,
  edit, test/preview, search, or delete** saved reports, or to write/validate SQL.
  Needs `sql-queries:create` / `:update` / `:delete` / `:read`.
  Cues: "create a report", "add a parameter", "test this query before saving", "delete the
  old report".

- **vc-sql-queries-business** — *run & export only.* Use when the request is to **get data
  from a report that already exists**: find it, run it with values, read the rows, export to
  CSV/XLSX/PDF/HTML. Needs `sql-queries:access` + `sql-queries:read`. Never writes SQL.
  Cues: "run the X report", "what does the orders report show", "export that to Excel".

If a "run" request can't be satisfied because no matching report exists, the business skill
hands off: it tells the user to ask an admin (use vc-sql-queries-admin) to create one.

## Shared rules that apply to both

- Reports run against a **read-only** connection string prefixed `SqlQueries.` — never author
  or run mutating SQL or DDL.
- Parameterize inputs; never string-concatenate values into the query text.
- Always send every declared parameter in a run/preview/export request — dropping one fails
  with `Must declare the scalar variable @<name>`. A blank/`null` value is fine: the backend
  substitutes a type default (`DateTime` → today, `Integer`/`Decimal` → `0`, `Boolean` →
  `false`), so an unset `DateTime` no longer causes a `SqlDateTime overflow`. A blank therefore
  means "use the default", not SQL `NULL`; a `(@FromDate IS NULL OR …)` guard is still sound
  defensive practice but won't return "all rows" for a blank date.
- The bearer token is a secret — never print, log, or save it.
- Confirm before persisted/destructive actions (create/update/delete) and before exporting a file.

## Environment setup (expected, configure locally — do not commit secrets)

Set these as environment variables or via your local Claude Code config; do not hard-code
them in the repo:

- `VC_PLATFORM_URL` — base platform URL (e.g. `https://localhost:5001`)
- `VC_USER`, `VC_PASS` — credentials used to obtain a bearer token

A read-only connection string named with the `SqlQueries.` prefix must exist in the platform
configuration before reports can run.

See each skill's `references/api.md` for the full endpoint table, model shapes, and
curl/PowerShell snippets.
