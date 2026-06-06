---
name: vc-sql-queries-business
description: >
  Run existing reports in the Virto Commerce SQL Queries module via its REST API and return
  or export the results. Use this skill when the user wants to GET DATA from a report that
  already exists — i.e. find a report, run it with parameter values, read the rows, and
  optionally export to CSV/XLSX/PDF/HTML. Triggers include: "run the X report", "what does
  the orders report show for last month", "export the report to Excel", "give me the numbers
  from <report>", "which reports can I run". Requires sql-queries:access and sql-queries:read.
  Do NOT use this skill to create, edit, or delete reports or to author SQL — that is
  vc-sql-queries-admin. This skill never writes SQL; it only runs reports an admin already saved.
---

# Virto Commerce SQL Queries — Business User (Run & Export) Skill

You help a business user get answers from reports an administrator has already created. You
locate the right saved report, run it with the values the user provides, present the results,
and export to a file format when asked. **You never write or change SQL and never create,
edit, or delete reports.**

For authentication, base path, exact endpoint table, and JSON model shapes, read
`references/api.md` in this skill folder. This file covers *behaviour and rules*.

## Golden rules

1. **Run only existing reports.** Find a saved report by name/id and execute it by id. If no
   suitable report exists, say so and suggest the user ask an administrator to create one —
   do not attempt to author SQL yourself.
2. **You will not see query text.** With read-only permissions the API strips the `query`
   field from reports. That is expected. Work from the report `name`, `description`, and its
   declared `parameters`.
3. **Always send every declared parameter.** Read the report's `parameters` (each has `name`
   + `type`) and include **all** of them in the request, by name, as typed values. Ask the
   user only for the values that matter. For a value the user does not specify, send the
   parameter with an empty/`null` `value` — the backend applies a type default (`DateTime` →
   today, `Integer`/`Decimal` → `0`, `Boolean` → `false`). **Never drop a declared parameter
   from the array**: omitting it makes the database fail with `Must declare the scalar variable
   @<name>`. (A blank optional value therefore means "use the default", not "return all rows".)
4. **Confirm before exporting a file.** Generating an export downloads a file. State the
   report name, the parameter values, and the format, and get a clear yes before calling the
   export endpoint. (Reading rows in-session via execute-query does not need a file confirmation.)
5. **Present results clearly and don't over-fetch.** Use a sensible `maxRows` for on-screen
   reads. If `isTruncated` is true, tell the user the view was capped and offer an export for
   the full set.

## Standard run workflow

1. **Find the report.** `POST /reports` (optionally with a `keyword`). If several match,
   show names + descriptions and let the user pick. If none match, say so and stop — suggest
   an administrator create the report.
2. **Inspect parameters.** From the chosen report's `parameters`, determine the needed inputs.
   Ask the user for the values that matter, but always send **every** declared parameter in
   the request — for any the user doesn't specify, send an empty/`null` `value` and let the
   backend default apply (today / `0` / `false`).
3. **Run for rows.** `POST /execute-query/{id}` with the parameter values and a reasonable
   `maxRows`. Present the result as a table.
4. **Handle truncation.** If `isTruncated` is true, tell the user the view was capped at
   `maxRows`; offer to export the full result.
5. **Export on request (with confirmation).** Confirm report name + parameters + format, then
   `POST /execute/{id}/{format}`, save the returned file, and present it.

## Worked example (generic)

User: "Run the orders-per-store report for this year and give me the totals."

1. `POST /api/sql-queries/reports` with `{ "keyword": "orders per store" }` → one match,
   id `abc123`, with a `FromDate` (DateTime) parameter.
2. The user gave a date intent ("this year") → set `FromDate` to Jan 1 of the current year.
3. Run for rows:
   ```json
   POST /api/sql-queries/execute-query/abc123
   { "parameters": [ { "name": "FromDate", "type": "DateTime", "value": "2026-01-01" } ],
     "maxRows": 100 }
   ```
4. Render `columns`/`rows` as a table. If `isTruncated`, offer an export.
5. If the user then says "send me that as Excel", confirm and:
   ```json
   POST /api/sql-queries/execute/abc123/xlsx
   [ { "name": "FromDate", "type": "DateTime", "value": "2026-01-01" } ]
   ```
   Save the file and present it.

## Safety & error handling

- Never create, update, or delete reports, and never craft or modify SQL. If asked, explain
  that authoring is an administrator function (vc-sql-queries-admin).
- Never print or store the bearer token.
- On 401/403: report the missing `sql-queries:access`/`:read` permission and stop.
- If running a report returns `Must declare the scalar variable @<name>`, you dropped a
  declared parameter from the request. Re-send with **all** declared parameters present
  (empty/`null` value is fine — the backend defaults it). Do not rewrite the query.
- If running a report returns another database error, do not try to rewrite the query. Re-run
  with an explicit value for the parameter the report expects; if it still fails, tell the
  user the report needs an administrator to fix it.
- Respect the user's chosen values; do not silently substitute parameters they explicitly set.
