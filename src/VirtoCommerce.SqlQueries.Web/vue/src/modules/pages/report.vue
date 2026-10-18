<template>
  <VcBlade
    :title="title"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="70%"
  >
    <VcContainer>
      <div class="sqlq-form">
        <VcBanner
          v-if="item.description"
          variant="info"
          icon="lucide-info"
        >
          <span class="sqlq-description">{{ item.description }}</span>
        </VcBanner>

        <VcCard
          v-if="parameters.length"
          :header="$t('SQL_QUERIES.REPORT.PARAMETERS', { count: parameters.length })"
          is-collapsable
        >
          <div class="sqlq-card sqlq-values">
            <ParamValue
              v-for="p in parameters"
              :key="p.name"
              v-model="p.value"
              :type="p.type"
              :label="`${p.name} (${p.type})`"
            />
          </div>
        </VcCard>

        <div class="sqlq-run">
          <VcInput
            v-model="maxRows"
            class="sqlq-run__rows"
            type="number"
            min="1"
            max="1000"
            :label="$t('SQL_QUERIES.RUN.MAX_ROWS')"
          />
          <VcSelect
            v-if="formats.length"
            v-model="format"
            class="sqlq-run__format"
            :label="$t('SQL_QUERIES.REPORT.FORMAT')"
            :options="formats"
            option-value="id"
            option-label="title"
            :clearable="false"
          />
        </div>

        <QueryResults
          :result="result"
          :error="error"
          :loading="running"
        />
      </div>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
// report-view: run a saved query with parameter values (runs on open), see the rows, export them to a file
import { useApiClient, useAsync, useBlade, type IBladeToolbar } from "@vc-shell/framework";
import { VcBanner, VcBlade, VcCard, VcContainer, VcInput, VcSelect } from "@vc-shell/framework/ui";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import {
  SqlQueriesClient,
  type SqlQuery,
  type SqlQueryParameter,
} from "../../api_client/virtocommerce.sqlqueries";
import ParamValue from "../components/ParamValue.vue";
import QueryResults from "../components/QueryResults.vue";
import { downloadReport, errorText, useQueryRun, withDefault } from "../run";

defineBlade({
  url: "/sql-query-report",
  name: "SqlQueryReport",
  permissions: ["sql-queries:read"],
});

const { t } = useI18n({ useScope: "global" });
const { param, setError, clearError } = useBlade();
const { getApiClient } = useApiClient(SqlQueriesClient);

const item = ref<SqlQuery>({});
const parameters = ref<SqlQueryParameter[]>([]);
const formats = ref<{ id: string; title: string }[]>([]);
const format = ref<string>();
const title = computed(() => t("SQL_QUERIES.REPORT.TITLE", { name: item.value.name ?? "" }));

const { maxRows, result, error, running, run } = useQueryRun(async (rows) =>
  (await getApiClient()).executeQueryById(param.value!, { parameters: parameters.value, maxRows: rows }),
);

const { loading, action: load } = useAsync(async () => {
  const client = await getApiClient();
  const [query, list] = await Promise.all([client.getById(param.value!), client.getReportFormats()]);
  item.value = query;
  parameters.value = (query.parameters ?? []).map((p) => ({ name: p.name, type: p.type, value: withDefault(p.type) }));
  formats.value = (list ?? []).map((x) => ({ id: x, title: x }));
  format.value ??= list?.[0];
});
load().then(() => run());

const { loading: exporting, action: exportReport } = useAsync(async () => {
  clearError();
  try {
    await downloadReport(param.value!, format.value!, parameters.value);
  } catch (e) {
    setError(errorText(e, t("SQL_QUERIES.RUN.FAILED")));
  }
});

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "run",
    title: t("SQL_QUERIES.TOOLBAR.RUN"),
    icon: "lucide-play",
    disabled: computed(() => running.value || loading.value),
    clickHandler: () => run(),
  },
  {
    id: "export",
    title: t("SQL_QUERIES.TOOLBAR.EXPORT"),
    icon: "lucide-download",
    disabled: computed(() => exporting.value || loading.value || !format.value),
    clickHandler: () => exportReport(),
  },
]);
</script>
