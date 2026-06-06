# SQL Queries Module — REST API Reference

Verified against `VirtoCommerce.SqlQueries.Web/Controllers/Api/SqlQueriesController.cs`
and the Core model classes. Field names and routes below are taken from source.

## Authentication

Acquire a platform bearer token once and reuse it:

```
POST {platformUrl}/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password&username={user}&password={password}
```

Send the returned `access_token` as `Authorization: Bearer {token}` on every call.
Treat the token as a secret: never print, log, or write it to a file. On 401/403, stop and
report the missing permission — do not retry blindly.

## Base path

```
{platformUrl}/api/sql-queries
```

## Endpoints

| Action | Method & route | Permission |
|---|---|---|
| Get one report by id | `GET /{id}` | `sql-queries:read` |
| Search reports | `POST /search` | `sql-queries:read` |
| List reports (query text stripped) | `POST /reports` | `sql-queries:read` |
| Create report | `POST /` | `sql-queries:create` |
| Update report | `PUT /` | `sql-queries:update` |
| Delete report(s) | `DELETE /?ids={id}&ids={id}` | `sql-queries:delete` |
| Preview / test an arbitrary query | `POST /execute-preview` | `sql-queries:create` AND `:update` |
| Run a saved report → rows (JSON) | `POST /execute-query/{id}` | `sql-queries:read` |
| Run a saved report → downloadable file | `POST /execute/{id}/{format}` | `sql-queries:read` |
| List export formats | `GET /formats` | (authenticated) |
| DB providers / connection info | `GET /database-information` | `sql-queries:read` |

Notes:
- `GET /{id}` and `POST /search` blank out the `query` text for callers lacking
  create/update permission. This is expected for business users.
- `POST /reports` always strips `query` text (it's the run-only listing).

## Permission names (from module.manifest localization)

`sql-queries:access`, `sql-queries:create`, `sql-queries:read`, `sql-queries:update`,
`sql-queries:delete`, `sql-queries:reports`.

## Model shapes (exact field names)

### SqlQuery (the saved report)
```json
{
  "id": "string (omit on create)",
  "name": "string (required)",
  "description": "string",
  "query": "string (required, the SQL text)",
  "connectionStringName": "string (required, e.g. 'SqlQueries.VirtoCommerce')",
  "parameters": [ { "name": "FromDate", "type": "DateTime" } ]
}
```
Also carries audit fields on read: `createdBy`, `createdDate`, `modifiedBy`, `modifiedDate`.

### SqlQueryParameter
```json
{ "id": "string (optional)", "name": "string", "type": "string", "value": <any> }
```
- On a **saved report** a parameter needs only `name` + `type` (it defines the parameter).
- When **previewing/running** a parameter carries a `value`.
- Parameter `type` values (per the module): `ShortText`, `DateTime`, `Boolean`, `Integer`,
  `Decimal`. **Verify canonical casing in this environment** (the type is stored as a free
  string); confirm via the parameter-type dropdown or an existing saved report.

### SqlQueryPreviewRequest — body for `POST /execute-preview`
```json
{
  "query": "string",
  "connectionStringName": "string",
  "parameters": [ { "name": "FromDate", "type": "DateTime", "value": "2026-01-01" } ],
  "maxRows": 100
}
```

### SqlQueryExecuteRequest — body for `POST /execute-query/{id}`
```json
{
  "parameters": [ { "name": "FromDate", "type": "DateTime", "value": "2026-01-01" } ],
  "maxRows": 100
}
```

### SqlQueryExecuteResult — response of preview and execute-query
```json
{
  "columns": [ { "name": "StoreId", "type": "string" } ],
  "rows": [ ["Store-A", 1240] ],
  "totalRowCount": 2,
  "isTruncated": false,
  "executionTimeMs": 35
}
```
Render `rows` against `columns`. Use `isTruncated` to warn that the view was capped at `maxRows`.

### Run-for-file — `POST /execute/{id}/{format}`
**IMPORTANT — different body shape.** The body is the **bare parameter list**, not wrapped
in an object:
```json
[ { "name": "FromDate", "type": "DateTime", "value": "2026-01-01" } ]
```
`{format}` is one of the values from `GET /formats` (typically `html`, `pdf`, `csv`, `xlsx`).
The response is the file bytes; the platform names it `{ReportName}_{yyyy-MM-dd}.{format}`.

### Search criteria — body for `POST /search` and `POST /reports`
Standard Virto search criteria. An empty body `{}` returns the default page. Narrow with a
keyword and paging, e.g. `{ "keyword": "orders", "skip": 0, "take": 50 }`. Response is
`{ "results": [SqlQuery], "totalCount": <int> }`.

## curl quick reference

```bash
# 1. token
TOKEN=$(curl -s -X POST "$PLATFORM/connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=$USER&password=$PASS" | jq -r .access_token)

# 2. list runnable reports
curl -s -X POST "$PLATFORM/api/sql-queries/reports" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"keyword":"orders","take":50}'

# 3. run a report for rows
curl -s -X POST "$PLATFORM/api/sql-queries/execute-query/$ID" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"parameters":[{"name":"FromDate","type":"DateTime","value":"2026-01-01"}],"maxRows":100}'

# 4. export to xlsx (note: bare array body)
curl -s -X POST "$PLATFORM/api/sql-queries/execute/$ID/xlsx" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '[{"name":"FromDate","type":"DateTime","value":"2026-01-01"}]' \
  -o report.xlsx
```

## PowerShell quick reference (Windows)

```powershell
$body = "grant_type=password&username=$env:VC_USER&password=$env:VC_PASS"
$token = (Invoke-RestMethod -Method Post -Uri "$Platform/connect/token" `
  -ContentType "application/x-www-form-urlencoded" -Body $body).access_token
$headers = @{ Authorization = "Bearer $token" }

# run a report for rows
Invoke-RestMethod -Method Post -Uri "$Platform/api/sql-queries/execute-query/$id" `
  -Headers $headers -ContentType "application/json" `
  -Body '{"parameters":[{"name":"FromDate","type":"DateTime","value":"2026-01-01"}],"maxRows":100}'
```
