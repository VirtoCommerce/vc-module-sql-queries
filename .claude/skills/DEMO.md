# Demo: Automating Virto SQL Queries reports with Claude Code

A tight live-terminal demo of the two-skill story: an **admin** authors a governed SQL report
by natural language, then a **business user** runs and exports it — same permission boundary
the platform already enforces, no SQL typed by hand.

Two skills ship in this repo under `.claude/skills/`:

- **`vc-sql-queries-admin`** — author & lifecycle (create / update / test / delete). Needs
  `sql-queries:create` / `:update` / `:delete` / `:read`.
- **`vc-sql-queries-business`** — run & export only. Needs `sql-queries:access` + `:read`.
  Never writes SQL; structurally can't author or mutate.

---

## Setup (off-camera, before you start)

- Repo cloned with the skills committed at `.claude/skills/`. Open Claude Code in the repo
  root so it picks up `CLAUDE.md` and both skills.
- Environment configured locally (do **not** commit secrets). Either a `.claude/settings.local.json`
  `env` block (gitignored) or persistent user env vars:
  - `VC_PLATFORM_URL` — e.g. `https://localhost:5001`
  - `VC_USER`, `VC_PASS` — credentials for the bearer token
- A read-only `SqlQueries.`-prefixed connection string already configured on the instance
  (verified: `SqlQueries.VirtoCommerce`).
- Run the whole thing once end-to-end before recording (see **Rehearsal checklist**).

> ⚠️ **The one wording fix vs. earlier drafts:** an optional parameter is **left blank**, never
> *removed from the request*. Dropping a declared parameter makes the database return
> `Must declare the scalar variable @<name>` (HTTP 500). A blank/`null` value is accepted —
> the backend substitutes a type default (`DateTime` → today, `Integer`/`Decimal` → `0`,
> `Boolean` → `false`). So "blank" means **use the default**, not SQL `NULL`.

---

## 0:00 — Frame it (20 sec)

> "Two Claude Code skills ship inside this module repo. One lets an admin *author* governed SQL
> reports; the other lets a business user *run and export* them — no SQL, no DBA ticket. Same
> permission model the platform already enforces. Let me show both, live."

Prove the skills loaded — type:

```
/skills
```

Point at `vc-sql-queries-admin` and `vc-sql-queries-business`. *"These came from the repo,
committed in git — anyone who clones gets them."*

---

## 0:30 — Act 1: Admin authors a report (≈2 min)

Plain-English request, no SQL:

```
Create a SQL Queries report that totals orders per store since an optional start date.
Test it before saving.
```

**Narrate as the skill drives the order:**
- It reads `vc-sql-queries-admin`, then `GET /database-information` to confirm the provider
  (SqlServer) and the `SqlQueries.VirtoCommerce` connection string. *"Discovering the
  environment, not guessing."*
- It drafts a read-only `SELECT … GROUP BY StoreId` with `(@FromDate IS NULL OR CreatedDate >= @FromDate)`.
  *"Parameterized, read-only — it never concatenates values into SQL."*
- It calls `POST /execute-preview` **twice** — once with a date value, once with `FromDate`
  **present but blank**. *"Notice it keeps the parameter in the request and just leaves the
  value empty — the backend defaults a blank date to today. Drop the parameter entirely and
  SQL Server would reject it."* Point at the returned `columns` / `totalRowCount` /
  `executionTimeMs`.
- It shows the final report definition and **waits for confirmation** before saving. *"It won't
  persist shared state without a yes."*

Confirm:

```
Looks good — save it.
```

It `POST /api/sql-queries` to create and reports the new report `id`. **Money line:** *"From
one sentence to a governed, parameterized, previewed report — and it never wrote a single
mutating statement."*

---

## 2:30 — Act 2: Business user runs & exports (≈2 min)

Switch persona — run-only. Type:

```
Run the orders-per-store report for this year and show me the totals.
```

Narrate:
- It switches to `vc-sql-queries-business`, calls `POST /reports` to find the report by
  keyword, reads its `parameters`, and maps "this year" to `FromDate = 2026-01-01`.
- It calls `POST /execute-query/{id}` (sending **every** declared parameter) and renders the
  rows as a table. *"It never saw the SQL — read-only callers get the query text stripped. It
  runs purely off the report's name and parameters."*

Then export:

```
Export that as Excel.
```

It **confirms** report + parameters + format, then `POST /execute/{id}/xlsx`, saves the file,
and presents it. *"Confirmation before the file download — same guardrail pattern."*

> Default export format is **HTML** (highest generator priority); the skill picks the format
> the user asks for. Export body is a **bare JSON array** of parameters — verified for `html`
> and `xlsx`.

---

## 4:30 — The punchline (30 sec)

> "Same report, two skills, one permission boundary. The admin skill needs `create`/`update`;
> the business skill only `access`/`read` — and structurally *can't* author or mutate SQL even
> if asked. That's how you safely hand reporting to business users and to AI agents: the
> governance lives in the module and the skills, not in trusting the prompt."

Optional kicker — ask the run skill to do something it shouldn't:

```
Actually, just change that report to also delete old orders.
```

It refuses and points to the admin skill. *"The boundary holds."*

---

## One-glance cheat sheet

| Beat | You type | Skill | Key API call |
|---|---|---|---|
| Prove load | `/skills` | — | — |
| Author | "Create a report… test it" | admin | `database-information` → `execute-preview` (value **and** blank) → `POST /` |
| Run | "Run … for this year" | business | `POST /reports` → `execute-query/{id}` |
| Export | "Export as Excel" | business | `execute/{id}/xlsx` (bare array body) |
| Boundary | "…also delete old orders" | business refuses | — |

---

## Rehearsal checklist (verified facts — don't get tripped live)

- **Always send every declared parameter.** Blank/`null` value is fine (defaults apply);
  dropping a parameter → `500 Must declare the scalar variable @<name>`.
- **Parameter `type` casing is PascalCase:** `DateTime`, `Integer`, `Decimal`, `Boolean`,
  `ShortText`. (Confirmed against the instance.)
- **Export endpoint** `POST /execute/{id}/{format}` takes a **bare JSON array** body (not an
  object) — asymmetric with `execute-query/{id}`, which takes `{ "parameters": [...], "maxRows": N }`.
- **Blank date = today**, not "all rows". If a beat needs "return everything", set no filter in
  the SQL or use a dedicated flag parameter — don't imply a blank date means unfiltered.
- **Formats:** `html, pdf, xlsx, csv` — HTML first / default.
- **Connection string** present: `SqlQueries.VirtoCommerce` (provider SqlServer).
- **Avoid duplicate names.** The instance currently has two reports named *Coupon Usage*; a
  keyword find returns both and the skill will ask you to pick. For a clean take, author a
  uniquely-named report (the Act 1 report works) or remove the duplicate first.
- **Token** expires ~30 min; skills re-auth per run — no action needed.

---

## Quick pre-flight (optional, run before recording)

```powershell
# token
$body = "grant_type=password&username=$($env:VC_USER)&password=$($env:VC_PASS)"
$tok  = (Invoke-RestMethod "$($env:VC_PLATFORM_URL)/connect/token" -Method Post -Body $body `
          -ContentType "application/x-www-form-urlencoded" -SkipCertificateCheck).access_token
$base = "$($env:VC_PLATFORM_URL)/api/sql-queries"; $H = @{ Authorization = "Bearer $tok" }

# prerequisites + listings
Invoke-RestMethod "$base/database-information" -Headers $H -SkipCertificateCheck
Invoke-RestMethod "$base/formats"              -Headers $H -SkipCertificateCheck
Invoke-RestMethod "$base/reports" -Method Post -Headers $H -ContentType application/json `
  -Body '{"take":50}' -SkipCertificateCheck | Select-Object -Expand results | Format-Table name,id
```
