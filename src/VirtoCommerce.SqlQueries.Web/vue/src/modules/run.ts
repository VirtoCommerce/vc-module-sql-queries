// Shared by the query details (test panel) and the report view: parameter defaults, the run state, error text,
// file download.
import { useAsync } from "@vc-shell/framework";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import type { SqlQueryExecuteResult } from "../api_client/virtocommerce.sqlqueries";

export const PARAMETER_TYPES = ["ShortText", "DateTime", "Boolean", "Integer", "Decimal"];

/** legacy defaults for an empty parameter value: today (DateTime), 0 (numbers), false (Boolean) */
export function withDefault(type?: string, value?: unknown): unknown {
  if (value !== undefined && value !== null && value !== "") return value;
  switch (type) {
    case "DateTime": {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      return today;
    }
    case "Integer":
    case "Decimal":
      return 0;
    case "Boolean":
      return false;
  }
  return value;
}

/** the server's message (an ApiException carries the response body) without the Development stack trace */
export function errorText(e: unknown, fallback: string): string {
  const response = (e as { response?: string } | undefined)?.response;
  try {
    const message = JSON.parse(response ?? "")?.message;
    if (message) return String(message).split(/\r?\n\s+at /)[0];
  } catch {
    // not JSON
  }
  return response || (e as Error | undefined)?.message || fallback;
}

/** run a query with the entered max rows (legacy default 100); the server error is shown instead of a toast */
export function useQueryRun(execute: (maxRows: number) => Promise<SqlQueryExecuteResult>) {
  const { t } = useI18n({ useScope: "global" });
  const maxRows = ref<number | string>(100);
  const result = ref<SqlQueryExecuteResult>();
  const error = ref<string>();
  const { loading: running, action: run } = useAsync(async () => {
    result.value = undefined;
    error.value = undefined;
    try {
      result.value = await execute(Number(maxRows.value) || 100);
    } catch (e) {
      error.value = errorText(e, t("SQL_QUERIES.RUN.FAILED"));
    }
  });
  return { maxRows, result, error, running, run };
}

/** POST the report parameters, save the returned file (name from Content-Disposition, like the legacy blade) */
export async function downloadReport(id: string, format: string, parameters: unknown[]) {
  const res = await fetch(`/api/sql-queries/execute/${encodeURIComponent(id)}/${encodeURIComponent(format)}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(parameters),
  });
  if (!res.ok) throw Object.assign(new Error(`${res.status}`), { response: await res.text() });
  const disposition = res.headers.get("content-disposition") ?? "";
  const match = /filename\*?=(?:UTF-8'')?((['"]).*?\2|[^;\n]*)/i.exec(disposition);
  const fileName = match?.[1] ? decodeURIComponent(match[1].replace(/['"]/g, "")) : `report.${format}`;
  const url = URL.createObjectURL(await res.blob());
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  // revoked right after click(), Firefox and Safari may not have started the download yet
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
