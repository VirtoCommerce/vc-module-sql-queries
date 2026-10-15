<template>
  <VcDatePicker
    v-if="type === 'DateTime'"
    v-model="value"
    :label="label"
  />
  <VcSwitch
    v-else-if="type === 'Boolean'"
    v-model="value"
    :label="label"
  />
  <VcInput
    v-else-if="type === 'Integer' || type === 'Decimal'"
    v-model="value"
    type="number"
    :step="type === 'Integer' ? '1' : 'any'"
    :label="label"
    placeholder="0"
  />
  <VcInput
    v-else
    v-model="value"
    :label="label"
    :placeholder="label"
  />
</template>

<script setup lang="ts">
// a parameter's value input by its type (legacy: text / smart-float / datepicker / switch)
import { VcDatePicker, VcInput, VcSwitch } from "@vc-shell/framework/ui";

defineProps<{ type?: string; label?: string }>();
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const value = defineModel<any>();
</script>
