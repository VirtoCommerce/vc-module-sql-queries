<template>
  <div class="sqlq-results">
    <div
      v-if="result && !loading"
      class="sqlq-results__meta"
    >
      <span>{{ $t("SQL_QUERIES.RUN.ROWS_RETURNED", { count: result.rows?.length ?? 0 }) }}</span>
      <span v-if="result.isTruncated">({{ $t("SQL_QUERIES.RUN.TRUNCATED") }})</span>
      <span class="sqlq-results__sep">|</span>
      <span>{{ $t("SQL_QUERIES.RUN.EXECUTION_TIME", { ms: result.executionTimeMs ?? 0 }) }}</span>
    </div>
    <VcBanner
      v-if="error"
      variant="danger"
      icon="lucide-triangle-alert"
    >
      <span class="sqlq-results__error">{{ error }}</span>
    </VcBanner>
    <div
      v-if="!loading && result?.rows?.length"
      class="sqlq-results__table"
    >
      <VcDataTable
        :items="rows"
        data-key="__row"
      >
        <VcColumn
          v-for="(column, i) in columns"
          :id="column.key"
          :key="`${column.key}-${column.title}`"
          :title="column.title"
          :mobile-role="i === 0 ? 'title' : 'field'"
          :always-visible="i === 0"
          min-width="100px"
        >
          <!-- values as returned (a number cell would round decimals and show null as "Not set");
               the number class keeps the column label on phones -->
          <template #body="{ data }">
            <span
              v-if="column.numeric && data[column.key] !== null && data[column.key] !== undefined"
              class="vc-table-cell-number"
            >{{ data[column.key] }}</span>
            <template v-else>{{ data[column.key] ?? "" }}</template>
          </template>
        </VcColumn>
      </VcDataTable>
    </div>
    <div
      v-else-if="result && !loading && !error"
      class="sqlq-results__empty"
    >
      {{ $t("SQL_QUERIES.RUN.NO_RESULTS") }}
    </div>
  </div>
</template>

<script setup lang="ts">
// results of a query run: metadata (rows, truncated, time), the server error, the rows as a table
import { VcBanner, VcColumn, VcDataTable } from "@vc-shell/framework/ui";
import { computed } from "vue";
import type { SqlQueryExecuteResult } from "../../api_client/virtocommerce.sqlqueries";

const props = defineProps<{ result?: SqlQueryExecuteResult; error?: string; loading?: boolean }>();

const NUMERIC = new Set(["Byte", "Int16", "Int32", "Int64", "Decimal", "Double", "Single"]);

const columns = computed(() =>
  (props.result?.columns ?? []).map((c, i) => ({
    key: `c${i}`,
    title: c.name || `#${i + 1}`,
    numeric: NUMERIC.has(c.type ?? ""),
  })),
);

const rows = computed(() =>
  (props.result?.rows ?? []).map((row, r) =>
    Object.fromEntries([
      ["__row", String(r)],
      ...(row as unknown[]).map((v, i) => [`c${i}`, v !== null && typeof v === "object" ? JSON.stringify(v) : v]),
    ]),
  ),
);
</script>

<style lang="scss">
.sqlq-results {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;

  &__meta {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    font-size: 13px;
    color: var(--color-neutral-500);
  }

  &__sep {
    color: var(--color-neutral-300);
  }

  &__error {
    white-space: pre-wrap;
    word-break: break-word;
  }

  &__table {
    height: 420px;
    min-height: 0;
    display: flex;
    flex-direction: column;
    border: 1px solid var(--color-neutral-200);
    border-radius: 8px;
    overflow: hidden;
  }

  &__empty {
    padding: 16px 0;
    color: var(--color-neutral-500);
  }
}
</style>
