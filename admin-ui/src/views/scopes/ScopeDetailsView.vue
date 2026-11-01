<script setup lang="ts">
import { type ResourceServerDto, type ScopeDto } from '@/api/generated'
import { getResourceServerById } from '@/api/resourceServerApi'
import { getScopeById } from '@/api/scopeApi'
import { RouteNames } from '@/router'
import { ref, watch } from 'vue'

const props = defineProps<{ id: string }>()
const scope = ref<ScopeDto | null>(null)
const resourceServer = ref<ResourceServerDto | null>(null)
const notFound = ref(false)
watch(
  () => props.id,
  async () => {
    scope.value = await getScopeById(props.id)
    notFound.value = scope.value === null
    if (scope.value?.resourceServerId) {
      resourceServer.value = await getResourceServerById(scope.value.resourceServerId)
    }
  },
  { immediate: true },
)
</script>
<template>
  <v-breadcrumbs
    :items="[{ title: 'Scopet', to: { name: RouteNames.ScopeList }, disabled: false }]"
  >
  </v-breadcrumbs>
  <div v-if="scope">
    <h1>Scopet <span class="text-tertiary">|</span> {{ scope.displayName }}</h1>

    <v-list class="details">
      <v-list-item>
        <v-list-item-subtitle>Nimi</v-list-item-subtitle>
        <v-list-item-title>{{ scope.name }}</v-list-item-title>
        <template #append>
          <v-btn
            color="primary"
            variant="text"
            prepend-icon="mdi-pencil"
            size="small"
            :to="{ name: RouteNames.ScopeEdit, params: { id: props.id } }"
            >Muokkaa</v-btn
          >
        </template>
      </v-list-item>

      <v-list-item>
        <v-list-item-subtitle>Näytettävänimi </v-list-item-subtitle>
        <v-list-item-title>{{ scope.displayName }}</v-list-item-title>
      </v-list-item>

      <v-list-item>
        <v-list-item-subtitle>Resurssipalvelin </v-list-item-subtitle>
        <v-list-item-title>
          <router-link
            v-if="resourceServer"
            :to="{ name: RouteNames.ResourceServerDetails, params: { id: scope.resourceServerId } }"
            >{{ resourceServer.displayName }}</router-link
          >
          <span v-else class="d-inline-flex align-center">
            <v-icon icon="mdi-earth" size="small" class="mr-1" />
            Globaali scope
          </span>
        </v-list-item-title>
      </v-list-item>
    </v-list>
  </div>
  <v-alert class="not-found" v-if="notFound" type="warning" variant="tonal">
    Scopea ei löytynyt.
  </v-alert>
</template>
