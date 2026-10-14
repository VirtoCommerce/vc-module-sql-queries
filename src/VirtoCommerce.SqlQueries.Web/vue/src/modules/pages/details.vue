<template>
  <VcBlade
    :title="title"
    :toolbar-items="bladeToolbar"
    :loading="loading || saving"
    width="70%"
  >
    <VcContainer>
      <VcForm class="sqlq-form">
        <VcBanner
          v-if="info && !info.connectionStringNames?.length"
          variant="warning"
          icon="lucide-triangle-alert"
        >
          {{ $t("SQL_QUERIES.DETAILS.NO_CONNECTIONS") }}
        </VcBanner>
        <VcSelect
          v-model="item.connectionStringName"
          :label="$t('SQL_QUERIES.DETAILS.CONNECTION')"
          :placeholder="$t('SQL_QUERIES.DETAILS.CONNECTION_PLACEHOLDER')"
          :options="connections"
          option-value="id"
          option-label="title"
          :clearable="false"
          required
        />
        <VcInput
          v-model="item.name"
          :label="$t('SQL_QUERIES.DETAILS.NAME')"
          :placeholder="$t('SQL_QUERIES.DETAILS.NAME_PLACEHOLDER')"
          required
          :error="form.isModified.value && !item.name?.trim()"
        />
        <VcTextarea
          v-model="item.description"
          :label="$t('SQL_QUERIES.DETAILS.DESCRIPTION')"
          :placeholder="$t('SQL_QUERIES.DETAILS.DESCRIPTION_PLACEHOLDER')"
        />
        <!-- SQL: a monospace textarea keeps the text exactly as typed (legacy: CodeMirror with SQL highlighting) -->
        <VcTextarea
          v-model="item.query"
          class="sqlq-code"
          :label="$t('SQL_QUERIES.DETAILS.QUERY', { provider: info?.databaseProvider ?? '' })"
          :placeholder="$t('SQL_QUERIES.DETAILS.QUERY_PLACEHOLDER')"
        />

        <VcCard
          :header="$t('SQL_QUERIES.DETAILS.PARAMETERS', { count: item.parameters?.length ?? 0 })"
          is-collapsable
        >
          <template #actions>
            <VcButton
              size="sm"
              variant="secondary"
              icon="lucide-plus"
              :aria-label="$t('SQL_QUERIES.DETAILS.ADD_PARAMETER')"
              @click.stop="addParameter"
            >
              {{ $t("SQL_QUERIES.DETAILS.ADD_PARAMETER") }}
            </VcButton>
          </template>
          <div class="sqlq-card">
            <div
              v-if="!item.parameters?.length"
              class="sqlq-hint"
            >
              {{ $t("SQL_QUERIES.DETAILS.NO_PARAMETERS") }}
            </div>
            <div
              v-for="(p, i) in item.parameters"
              :key="i"
              class="sqlq-param"
            >
              <VcInput
                v-model="p.name"
                class="sqlq-param__name"
                :label="$t('SQL_QUERIES.DETAILS.PARAMETER_NAME')"
                required
                :error="!p.name?.trim()"
              />
              <VcSelect
                v-model="p.type"
                class="sqlq-param__type"
                :label="$t('SQL_QUERIES.DETAILS.PARAMETER_TYPE')"
                :options="types"
                option-value="id"
                option-label="title"
                :clearable="false"
              />
              <ParamValue
                :key="valueKey(p)"
                v-model="testValues[valueKey(p)]"
                class="sqlq-param__value"
                :type="p.type"
                :label="$t('SQL_QUERIES.DETAILS.PARAMETER_VALUE')"
              />
              <VcButton
                class="sqlq-param__remove"
                variant="secondary"
                icon="lucide-trash-2"
                :aria-label="$t('SQL_QUERIES.DETAILS.REMOVE_PARAMETER')"
                :title="$t('SQL_QUERIES.DETAILS.REMOVE_PARAMETER')"
                @click="item.parameters!.splice(i, 1)"
              />
            </div>
          </div>
        </VcCard>

        <VcCard :header="$t('SQL_QUERIES.DETAILS.TEST')">
          <div class="sqlq-card">
            <div class="sqlq-run">
              <VcInput
                v-model="maxRows"
                class="sqlq-run__rows"
                type="number"
                min="1"
                max="1000"
                :label="$t('SQL_QUERIES.RUN.MAX_ROWS')"
              />
              <VcButton
                icon="lucide-play"
                :loading="running"
                :disabled="running || !item.query?.trim() || !item.connectionStringName"
                @click="run()"
              >
                {{ $t("SQL_QUERIES.TOOLBAR.RUN") }}
              </VcButton>
            </div>
            <QueryResults
              :result="result"
              :error="error"
              :loading="running"
            />
          </div>
        </VcCard>
      </VcForm>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
// sql-query-details: create / edit a query (connection, name, description, SQL, parameters) and test-run it
import { useApiClient, useAsync, useBlade, useBladeForm, usePermissions, type IBladeToolbar } from "@vc-shell/framework";
import {
  VcBanner,
  VcBlade,
  VcButton,
  VcCard,
  VcContainer,
  VcForm,
  VcInput,
  VcSelect,
  VcTextarea,
} from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import {
  SqlQueriesClient,
  type DatabaseInformation,
  type SqlQuery,
  type SqlQueryParameter,
} from "../../api_client/virtocommerce.sqlqueries";
import ParamValue from "../components/ParamValue.vue";
import QueryResults from "../components/QueryResults.vue";
import { PARAMETER_TYPES, useQueryRun, withDefault } from "../run";

defineBlade({
  url: "/sql-query",
  name: "SqlQueryDetails",
  permissions: ["sql-queries:read"],
});

const { t } = useI18n({ useScope: "global" });
const { param, callParent, closeSelf } = useBlade();
const { getApiClient } = useApiClient(SqlQueriesClient);
const { hasAccess } = usePermissions();

const item = ref<SqlQuery>({ parameters: [] });
const info = ref<DatabaseInformation>();
const isNew = computed(() => !param.value);
const title = computed(() =>
  isNew.value ? t("SQL_QUERIES.DETAILS.NEW") : item.value.name || t("SQL_QUERIES.DETAILS.TITLE"),
);
const connections = computed(() => (info.value?.connectionStringNames ?? []).map((x) => ({ id: x, title: x })));
const types = PARAMETER_TYPES.map((x) => ({ id: x, title: x }));

const valid = computed(
  () =>
    !!item.value.name?.trim() &&
    !!item.value.connectionStringName &&
    (item.value.parameters ?? []).every((p) => !!p.name?.trim()),
);
const form = useBladeForm({
  data: item,
  canSaveOverride: valid,
  closeConfirmMessage: computed(() => t("SQL_QUERIES.DETAILS.CLOSE_CONFIRMATION")),
});

const { loading, action: load } = useAsync(async () => {
  const client = await getApiClient();
  const [query, database] = await Promise.all([
    isNew.value ? Promise.resolve<SqlQuery>({ parameters: [] }) : client.getById(param.value!),
    client.getDatabaseInformation(),
  ]);
  info.value = database;
  query.parameters ??= [];
  // preselect the only available connection string to save a click (legacy)
  if (!query.connectionStringName && database.connectionStringNames?.length === 1) {
    query.connectionStringName = database.connectionStringNames[0];
  }
  item.value = query;
  form.setBaseline();
});
load();

function addParameter() {
  const names = (item.value.parameters ?? []).map((p) => p.name);
  let index = 1;
  while (names.includes(`param${index}`)) index++;
  (item.value.parameters ??= []).push({ name: `param${index}`, type: "ShortText" });
}

// test values aren't part of the query: keyed by parameter name + type (a type change starts from that type's
// default), defaulted like the legacy blade
const testValues = ref<Record<string, unknown>>({});
const valueKey = (p: SqlQueryParameter) => `${p.name}|${p.type}`;
watch(
  () => (item.value.parameters ?? []).map(valueKey).join(),
  () => {
    for (const p of item.value.parameters ?? []) {
      testValues.value[valueKey(p)] = withDefault(p.type, testValues.value[valueKey(p)]);
    }
  },
  { immediate: true },
);

const { maxRows, result, error, running, run } = useQueryRun(async (maxRows) =>
  (await getApiClient()).executeQuery({
    query: item.value.query,
    connectionStringName: item.value.connectionStringName,
    parameters: (item.value.parameters ?? []).map((p) => ({
      name: p.name,
      type: p.type,
      value: testValues.value[valueKey(p)],
    })),
    maxRows,
  }),
);

const { loading: saving, action: save } = useAsync(async () => {
  const client = await getApiClient();
  if (isNew.value) {
    await client.create(item.value);
    form.setBaseline();
    callParent("reload");
    // legacy: a created query closes its blade
    await closeSelf();
    return;
  }
  item.value = await client.update(item.value);
  item.value.parameters ??= [];
  form.setBaseline();
  callParent("reload");
});

const bladeToolbar = ref<IBladeToolbar[]>([
  {
    id: "save",
    title: computed(() => t(isNew.value ? "SQL_QUERIES.TOOLBAR.CREATE" : "SQL_QUERIES.TOOLBAR.SAVE")),
    icon: "lucide-save",
    isVisible: computed(() => hasAccess(isNew.value ? "sql-queries:create" : "sql-queries:update")),
    disabled: computed(() => !form.canSave.value || saving.value),
    clickHandler: () => save(),
  },
  {
    id: "reset",
    title: t("SQL_QUERIES.TOOLBAR.RESET"),
    icon: "lucide-undo-2",
    isVisible: computed(() => !isNew.value),
    disabled: computed(() => !form.isModified.value),
    clickHandler: () => form.revert(),
  },
]);
</script>
