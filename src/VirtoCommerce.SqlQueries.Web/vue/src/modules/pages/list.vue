<template>
  <VcBlade
    :title="$t('SQL_QUERIES.LIST.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:sort-field="sortField"
      v-model:sort-order="sortOrder"
      v-model:selection="selection"
      :selection-mode="hasAccess('sql-queries:delete') ? 'multiple' : undefined"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      state-key="SQL_QUERIES_LIST"
      :active-item-id="activeItemId"
      :show-all-columns="!activeItemId"
      :row-actions="rowActions"
      row-actions-mode="dropdown"
      row-actions-position="column"
      :empty-state="{
        icon: 'lucide-database',
        title: $t('SQL_QUERIES.LIST.EMPTY_TITLE'),
        description: $t('SQL_QUERIES.LIST.EMPTY_TEXT'),
      }"
      @row-click="(e: { data: SqlQuery }) => openReport(e.data)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="name"
        :title="$t('SQL_QUERIES.LIST.NAME')"
        mobile-role="title"
        sortable
        always-visible
      />
      <VcColumn
        id="description"
        :title="$t('SQL_QUERIES.LIST.DESCRIPTION')"
        mobile-role="field"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
// sql-query-list: saved queries; row click opens the report, row menu: view / edit / delete; toolbar add / delete
import {
  useApiClient,
  useAsync,
  useBlade,
  useDataTablePagination,
  useDataTableSort,
  useFunctions,
  usePermissions,
  usePopup,
  type IBladeToolbar,
  type TableAction,
} from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { SqlQueriesClient, type SqlQuery } from "../../api_client/virtocommerce.sqlqueries";

defineBlade({
  url: "/sql-queries",
  name: "SqlQueriesList",
  isWorkspace: true,
  permissions: ["sql-queries:access"],
  menuItem: { title: "SQL_QUERIES.MENU.TITLE", icon: "lucide-database-zap", priority: 105 },
});

const PAGE_SIZE = 20;
const { t } = useI18n({ useScope: "global" });
const { openBlade, closeChildren, exposeToChildren } = useBlade();
const { getApiClient } = useApiClient(SqlQueriesClient);
const { showConfirmation } = usePopup();
const { hasAccess } = usePermissions();
const { debounce } = useFunctions();

const items = ref<SqlQuery[]>([]);
const totalCount = ref(0);
const selection = ref<SqlQuery[]>([]);
const activeItemId = ref<string>();

const { sortField, sortOrder, sortExpression } = useDataTableSort({ stateKey: "SQL_QUERIES_LIST" });
const pagination = useDataTablePagination({ stateKey: "SQL_QUERIES_LIST", pageSize: PAGE_SIZE, totalCount });

const { loading, action: load } = useAsync(async () => {
  const r = await (await getApiClient()).search({ sort: sortExpression.value, skip: pagination.skip, take: PAGE_SIZE });
  items.value = r.results ?? [];
  totalCount.value = r.totalCount ?? 0;
});

load();
watch([sortExpression, () => pagination.skip], debounce(load, 300));

function open(name: string, item?: SqlQuery) {
  openBlade({
    name,
    param: item?.id,
    onOpen: () => (activeItemId.value = item?.id),
    onClose: () => (activeItemId.value = undefined),
  });
}
const openReport = (item: SqlQuery) => open("SqlQueryReport", item);
const openDetails = (item?: SqlQuery) => open("SqlQueryDetails", item);

async function remove(list: SqlQuery[]) {
  if (!(await showConfirmation(t("SQL_QUERIES.ALERTS.DELETE_CONFIRMATION")))) return;
  await closeChildren();
  await (await getApiClient()).delete(list.map((x) => x.id!));
  selection.value = [];
  await load();
}

// legacy row context menu: View report / Edit report / Delete
const rowActions = (item: SqlQuery): TableAction[] => [
  { id: "view", icon: "lucide-eye", title: t("SQL_QUERIES.TOOLBAR.VIEW_REPORT"), type: "info", clickHandler: () => openReport(item) },
  {
    id: "edit",
    icon: "lucide-pencil",
    title: t("SQL_QUERIES.TOOLBAR.EDIT_REPORT"),
    type: "info",
    hidden: !hasAccess("sql-queries:update"),
    clickHandler: () => openDetails(item),
  },
  {
    id: "delete",
    icon: "lucide-trash-2",
    title: t("SQL_QUERIES.TOOLBAR.DELETE"),
    type: "danger",
    variant: "danger",
    hidden: !hasAccess("sql-queries:delete"),
    clickHandler: () => remove([item]),
  },
];

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("SQL_QUERIES.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "add",
    title: t("SQL_QUERIES.TOOLBAR.ADD"),
    icon: "lucide-plus",
    permissions: "sql-queries:create",
    clickHandler: () => openDetails(),
  },
  {
    id: "delete",
    title: t("SQL_QUERIES.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: "sql-queries:delete",
    disabled: computed(() => !selection.value.length),
    clickHandler: () => remove(selection.value),
  },
]);

exposeToChildren({ reload: () => load() });
</script>
