<!-- ResourceServerAutocomplete.vue -->
<script setup lang="ts">
import type { ResourceServerDto } from '@/api/generated'
import { getResourceServerById, getResourceServerPagedList } from '@/api/resourceServerApi'
import { ref, watch } from 'vue'

const {
  selectedId,
  errorMessages,
  disabled,
  required = false,
  label = 'Resurssipalvelin',
  hint = '',
  onChange,
} = defineProps<{
  selectedId?: string | null
  errorMessages?: string[]
  disabled?: boolean
  required?: boolean
  label?: string
  hint?: string
  onChange: (resourceServer: ResourceServerDto | null) => void
}>()

const serverOptions = ref<ResourceServerDto[]>([])
const selectedServer = ref<ResourceServerDto | null>(null)
const serversLoading = ref(false)
let loaded = false

async function searchServers(text: string) {
  serversLoading.value = true
  try {
    const result = await getResourceServerPagedList({
      pageNumber: 1,
      pageSize: 10,
      searchTerm: text || null,
    })
    serverOptions.value = result.items
  } finally {
    serversLoading.value = false
  }
}

function onMenuOpened(open: boolean) {
  if (open && !loaded) {
    loaded = true
    searchServers('')
  }
}

function onServerSelected(value: unknown) {
  const server = value as ResourceServerDto | null
  selectedServer.value = server
  onChange(server)
}

watch(
  () => selectedId,
  async (id) => {
    if (id === selectedServer.value?.id) return
    selectedServer.value = id ? await getResourceServerById(id) : null
  },
  { immediate: true },
)
</script>

<template>
  <v-autocomplete
    :model-value="selectedServer"
    :items="serverOptions"
    :loading="serversLoading"
    :disabled="disabled"
    :error-messages="errorMessages"
    return-object
    item-title="displayName"
    item-value="id"
    :label="label"
    no-filter
    clearable
    autocomplete="off"
    @update:model-value="onServerSelected"
    @update:search="searchServers"
    @update:menu="onMenuOpened"
    :hint="hint"
    :persistent-hint="!!hint"
    :rules="required ? [(v) => !!v || 'Pakollinen kenttä'] : []"
  />
</template>
