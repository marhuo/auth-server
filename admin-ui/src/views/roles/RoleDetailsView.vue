<script setup lang="ts">
import { type ResourceServerDto, type RoleDto } from '@/api/generated'
import { getResourceServerById } from '@/api/resourceServerApi'
import { getRoleById } from '@/api/roleApi'
import { RouteNames } from '@/router'
import { ref, watch } from 'vue'
const props = defineProps<{ id: string }>()
const role = ref<RoleDto | null>(null)
const resourceServer = ref<ResourceServerDto | null>(null)
const notFound = ref(false)

watch(
  () => props.id,
  async () => {
    role.value = await getRoleById(props.id)
    notFound.value = role.value === null
    if (role.value) {
      resourceServer.value = await getResourceServerById(role.value.resourceServerId)
    } else {
      notFound.value = true
    }
  },
  { immediate: true },
)
</script>
<template>
  <v-breadcrumbs :items="[{ title: 'Roolit', to: { name: RouteNames.RoleList }, disabled: false }]">
  </v-breadcrumbs>
  <div v-if="role && resourceServer">
    <h1>Rooli <span class="text-tertiary">|</span> {{ role.name }}</h1>

    <v-list class="details">
      <v-list-item>
        <v-list-item-subtitle>Nimi</v-list-item-subtitle>
        <v-list-item-title>{{ role.name }}</v-list-item-title>
        <template #append>
          <v-btn
            color="primary"
            variant="text"
            prepend-icon="mdi-pencil"
            size="small"
            :to="{ name: RouteNames.RoleEdit, params: { id: role.id } }"
            >Muokkaa</v-btn
          >
        </template>
      </v-list-item>

      <v-list-item>
        <v-list-item-subtitle>Kuvaus </v-list-item-subtitle>
        <v-list-item-title>{{ role.description }}</v-list-item-title>
      </v-list-item>

      <v-list-item>
        <v-list-item-subtitle>Resurssipalvelin </v-list-item-subtitle>
        <v-list-item-title>
          <router-link
            :to="{ name: RouteNames.ResourceServerDetails, params: { id: role.resourceServerId } }"
            >{{ resourceServer.displayName }}</router-link
          >
        </v-list-item-title>
      </v-list-item>
    </v-list>
  </div>

  <v-alert class="not-found" v-if="notFound" type="warning" variant="tonal">
    Roolia ei löytynyt.
  </v-alert>
</template>
