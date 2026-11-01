<script setup lang="ts">
import type { TenantDto } from '@/api/generated'
import { getTenantById } from '@/api/tenantApi'
import type { MenuItem } from '@/plugins/vuetifyTypes'
import { RouteNames } from '@/router'
import { formatIsoDateTime } from '@/utils/dateUtils'
import TenantSuspendDialog from '@/views/tenants/components/TenantSuspendDialog.vue'
import TenantUnsuspendDialog from '@/views/tenants/components/TenantUnsuspendDialog.vue'
import { ref, watch } from 'vue'
const props = defineProps<{ id: string }>()
const tenant = ref<TenantDto | null>(null)
const notFound = ref(false)
const showSuspendDialog = ref(false)
const showUnsuspendDialog = ref(false)
const menuItems = ref<MenuItem[]>([])

async function loadTenant(id: string) {
  const t = await getTenantById(id)
  tenant.value = t
  notFound.value = tenant.value === null
  if (tenant.value === null) return

  if (tenant.value.suspendedAt) {
    menuItems.value = [
      {
        title: 'Poista lukitus',
        value: 'unsuspend',
        prependIcon: 'mdi-lock-open',
        onClick: () => (showUnsuspendDialog.value = true),
      },
    ]
  } else {
    menuItems.value = [
      {
        title: 'Lukitse',
        value: 'suspend',
        prependIcon: 'mdi-lock',
        onClick: () => (showSuspendDialog.value = true),
      },
    ]
  }
}

watch(
  () => props.id,
  async () => {
    await loadTenant(props.id)
  },
  { immediate: true },
)
</script>
<template>
  <v-breadcrumbs
    :items="[{ title: 'Asiakkaat', to: { name: RouteNames.TenantList }, disabled: false }]"
  >
  </v-breadcrumbs>

  <div v-if="tenant">
    <h1>Asiakas <span class="text-tertiary">|</span> {{ tenant.name }}</h1>

    <v-list class="details">
      <v-list-item>
        <v-list-item-subtitle>Name</v-list-item-subtitle>
        <v-list-item-title>{{ tenant.name }}</v-list-item-title>
        <template #append>
          <div style="display: flex; gap: 20px; align-items: center">
            <v-btn
              color="primary"
              variant="text"
              prepend-icon="mdi-pencil"
              size="small"
              :to="{ name: RouteNames.TenantEdit, params: { id: tenant.id } }"
              >Edit</v-btn
            >
            <v-menu v-if="menuItems.length > 0" location="bottom end" scroll-strategy="close">
              <template #activator="{ props: activatorProps }">
                <v-icon-btn icon="mdi-dots-vertical" size="small" v-bind="activatorProps" />
              </template>
              <v-list :items="menuItems" item-props density="compact" slim class="py-0" />
            </v-menu>
          </div>
        </template>
      </v-list-item>

      <v-list-item>
        <v-list-item-subtitle>URL-tunniste (slug)</v-list-item-subtitle>
        <v-list-item-title>{{ tenant.slug }}</v-list-item-title>
      </v-list-item>

      <v-list-item>
        <v-list-item-subtitle>Luotu</v-list-item-subtitle>
        <v-list-item-title>{{ formatIsoDateTime(tenant.createdAt) }}</v-list-item-title>
      </v-list-item>

      <v-list-item v-if="tenant.suspendedAt">
        <v-list-item-subtitle>
          <v-icon color="error" icon="mdi-lock" size="small" class="mr-1" />
          Lukittu
        </v-list-item-subtitle>
        <v-list-item-title>{{ formatIsoDateTime(tenant.suspendedAt) }}</v-list-item-title>
        <v-list-item-title>{{ tenant.suspensionReason }}</v-list-item-title>
      </v-list-item>
    </v-list>
  </div>

  <v-alert class="not-found" v-if="notFound" type="warning" variant="tonal">
    Asiakasta ei löytynyt.
  </v-alert>

  <tenant-suspend-dialog
    v-if="tenant"
    :tenant="tenant"
    :open="showSuspendDialog"
    @cancel="showSuspendDialog = false"
    @suspended="
      () => {
        showSuspendDialog = false
        loadTenant(id)
      }
    "
  />

  <tenant-unsuspend-dialog
    v-if="tenant"
    :tenant="tenant"
    :open="showUnsuspendDialog"
    @cancel="showUnsuspendDialog = false"
    @unsuspended="
      () => {
        showUnsuspendDialog = false
        loadTenant(id)
      }
    "
  />
</template>
