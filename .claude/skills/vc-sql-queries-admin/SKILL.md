---
name: vc-sql-queries-admin
description: >
  Author and manage reusable reports in the Virto Commerce SQL Queries module via its
  REST API. Use this skill when the user wants to CREATE, UPDATE, TEST/PREVIEW, search,
  or DELETE saved SQL query reports — i.e. report authoring and lifecycle management for
  an administrator. Triggers include: "create a report", "add a SQL query", "edit the
  report query", "test this query before saving", "add a parameter to the report",
  "list all reports", "delete a report". Requires sql-queries:create / :update / :delete
  / :read permissions. Do NOT use this skill for merely running an existing report to get
  data — that is vc-sql-queries-business. Do NOT use it to design schema-level changes to
  the platform database.
---

# Virto Commerce SQL Queries — Administrator (Report Authoring) Skill

You manage the lifecycle of reusable SQL query reports through the SQL Queries module REST
API. Your job is to help an administrator design a safe, parameterized query, validate it
with a live preview, and persist it so business users can run it later.

For authentication, base path, exact endpoint table, and JSON model shapes, read
`references/api.md` in this skill folder. This file covers *behaviour and rules*.

## Golden rules

1. **Read-only data, by design.** Saved reports run against a dedicated **read-only**
   connection string whose name is prefixed with `SqlQueries.` (e.g. `SqlQueries.VirtoCommerce`).
   Never author a query that writes data (`INSERT`/`UPDATE`/`DELETE`/`MERGE`/DDL). Reports
   are for reading and aggregation only. If a user asks for a mutating query, refuse and
   explain that the module is read-only.
2. **Always preview before you save.** Use `POST /execute-preview` to validate syntax, column
   shape, and row count against real data BEFORE creating or updating a saved report.
3. **Parameterize, never interpolate.** Pass user-supplied values as `parameters`, never by
   string-concatenating values into the `query` text. The query references them with the
   provider's parameter syntax (`@Name` for SQL Server).
4. **Send every declared parameter; blanks become type defaults.** The backend adds a DB
   parameter only for entries present in the request, so a request must include **all**
   declared parameters — dropping one makes the database fail with `Must declare the scalar
   variable @<name>`. For a value left unspecified, send the parameter with an empty/`null`
   `value`: the backend substitutes a type default (`DateTime` → today, `Integer`/`Decimal` →
   `0`, `Boolean` → `false`). Because of this, an unset `DateTime` no longer causes a
   `SqlDateTime overflow`, **but** a blank value now means "use the default", not SQL `NULL`.
   A `(@FromDate IS NULL OR SomeDate >= @FromDate)` guard is still good defensive practice, yet
   its `IS NULL` branch will not fire for a blank `DateTime`/number/bool (it arrives as the
   default, not `NULL`). If a report genuinely needs an explicit "no filter / all rows" option,
   model it with a dedicated flag parameter rather than relying on a blank value.
5. **Confirm before destructive or persisted actions.** Creating, updating, and deleting
   saved reports change shared state other users depend on. State exactly what you are about
   to do (name, connection string, parameter list, and a one-line summary of the query) and
   get a clear yes before calling Create / Update / Delete. Never delete without explicit
   confirmation of the specific report name(s).

## Standard authoring workflow

1. **Clarify intent.** Confirm the business question, the tables, and which inputs should be
   parameters vs. fixed.
2. **Discover the environment.** `GET /database-information` to confirm the provider
   (SQL Server / MySQL / PostgreSQL) and the available `SqlQueries.`-prefixed connection
   string name. Provider affects parameter syntax and functions.
3. **Draft the SQL.** Read-only `SELECT` only. Parameterize inputs. Follow golden rule 4 for
   optional parameters (blanks arrive as type defaults, not `NULL`).
4. **Preview against real data.** `POST /execute-preview` with representative parameter values,
   AND once with optional params left **empty** (present in the array with an empty/`null`
   `value` — never removed), to prove both paths work and the default substitution behaves as
   intended. Inspect `columns`, `totalRowCount`, `isTruncated`, `executionTimeMs`. If execution
   time is high or the row count is huge, suggest adding a parameter or a date bound before saving.
5. **Show the user the final report definition** (name, connection string, parameters, query)
   and get confirmation.
6. **Persist.** `POST /` to create, or `PUT /` (include `id`) to update.
7. **Hand off.** Remind the user that business users need `sql-queries:access` +
   `sql-queries:read` to find and run it, and that running/exporting is vc-sql-queries-business.

## Worked example (generic)

User: "Create a report that totals orders per store since a given date, where the date is optional."

1. `GET /database-information` → provider = SqlServer, connection = `SqlQueries.VirtoCommerce`.
2. Draft (note the nullable guard on `@FromDate`):
   ```sql
   SELECT StoreId,
          COUNT(*)   AS TotalOrders,
          SUM(Total) AS TotalOrderAmount
   FROM CustomerOrder
   WHERE (@FromDate IS NULL OR CreatedDate >= @FromDate)
   GROUP BY StoreId
   ORDER BY StoreId;
   ```
3. Preview twice — once with `value` set, once with the parameter present but `value` empty
   (the backend defaults a blank `DateTime` to today) — to confirm both paths work. Do not
   remove the parameter from the array, or the database returns `Must declare the scalar
   variable @FromDate`.
4. Confirm with the user, then `POST /api/sql-queries` with the report definition.
5. Report the new `id` back to the user.

## Safety & error handling

- **Never** author or run mutating SQL or DDL. SELECT/CTE/aggregation only.
- **Never** embed secrets, raw connection strings, or credentials in the `query` text or in
  output. Reference connection strings by their configured name only.
- On `Must declare the scalar variable @<name>`: a declared parameter was dropped from the
  request. Re-send with all declared parameters present (empty/`null` value is fine — the
  backend defaults it). This is the most common run-time error now.
- On `SqlDateTime overflow`: a caller passed an explicit out-of-range date (blank values are
  defaulted to today and won't overflow). Re-preview with an in-range value; if a report needs
  to treat a blank date specially, model it explicitly rather than relying on `NULL`.
- On 401/403: surface the missing permission; do not retry blindly.
- If a preview returns a database error, show the user the database error verbatim (it is not
  sensitive) and propose a corrected query.
- Keep `maxRows` modest (default 100) for previews; previews validate, they don't bulk-export.
