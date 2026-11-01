<script setup lang="ts">
import type { ResourceServerDto, RoleDto, ScopeDto } from '@/api/generated'
import { getResourceServerById } from '@/api/resourceServerApi'
import { getRolePagedList } from '@/api/roleApi'
import { getScopePagedList } from '@/api/scopeApi'
import { usePagedList } from '@/composables/usePagedList'
import { RouteNames } from '@/router'
import CreateRoleDialog from '@/views/resource-servers/components/CreateRoleDialog.vue'
import CreateScopeDialog from '@/views/resource-servers/components/CreateScopeDialog.vue'
import { ref, watch } from 'vue'
const props = defineProps<{ id: string }>()
const breadcrumbs = [
  { title: 'Resurssipalvelimet', to: { name: RouteNames.ResourceServerList }, disabled: false },
]
const resourceServer = ref<ResourceServerDto | null>(null)
const notFound = ref(false)
const createRoleDialogOpen = ref(false)
const createScopeDialogOpen = ref(false)

const rolePagedList = usePagedList<RoleDto>({
  load: (request) => getRolePagedList({ ...request, resourceServerId: props.id }),
})

const scopePagedList = usePagedList<ScopeDto>({
  load: (request) => getScopePagedList({ ...request, resourceServerId: props.id }),
})

watch(
  () => props.id,
  async (id, oldId) => {
    resourceServer.value = await getResourceServerById(id)
    notFound.value = resourceServer.value === null
    if (oldId !== id) {
      rolePagedList.reload()
      scopePagedList.reload()
    }
  },
  { immediate: true },
)

function onRoleCreated() {
  createRoleDialogOpen.value = false
  rolePagedList.setPage(1)
  rolePagedList.reload()
}
function onScopeCreated() {
  createScopeDialogOpen.value = false
  scopePagedList.setPage(1)
  scopePagedList.reload()
}
</script>
<template>
  <v-breadcrumbs :items="breadcrumbs"></v-breadcrumbs>
  <div v-if="resourceServer">
    <h1>Resurssipalvelin <span class="text-tertiary">|</span> {{ resourceServer.displayName }}</h1>

    <v-list class="details">
      <v-list-item>
        <v-list-item-subtitle>Näyttönimi</v-list-item-subtitle>
        <v-list-item-title>{{ resourceServer.displayName }}</v-list-item-title>
        <template #append>
          <v-btn
            color="primary"
            variant="text"
            prepend-icon="mdi-pencil"
            size="small"
            :to="{ name: RouteNames.ResourceServerEdit, params: { id: resourceServer.id } }"
            >Muokkaa</v-btn
          >
        </template>
      </v-list-item>

      <v-list-item>
        <v-list-item-subtitle>Tunniste (audience)</v-list-item-subtitle>
        <v-list-item-title>{{ resourceServer.audience }}</v-list-item-title>
      </v-list-item>
    </v-list>

    <v-row class="justify-space-between align-center mt-8">
      <h2 class="px-4">Roolit</h2>
      <v-btn
        color="primary"
        variant="text"
        prepend-icon="mdi-plus"
        size="small"
        @click="createRoleDialogOpen = true"
        >Lisää uusi</v-btn
      >
    </v-row>
    <v-sheet class="pb-2">
      <v-table v-if="rolePagedList">
        <thead>
          <tr>
            <th>Nimi</th>
            <th>Kuvaus</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="role in rolePagedList.items" :key="role.id">
            <td>
              <router-link
                color="primary"
                :to="{ name: RouteNames.RoleDetails, params: { id: role.id } }"
                >{{ role.name }}
              </router-link>
            </td>
            <td>{{ role.description }}</td>
          </tr>
        </tbody>
      </v-table>
      <v-pagination
        v-if="rolePagedList && rolePagedList.totalPages > 1"
        :model-value="rolePagedList.pageNumber"
        :length="rolePagedList.totalPages"
        @update:model-value="(pageNumber) => rolePagedList.setPage(pageNumber)"
      />
    </v-sheet>

    <v-row class="justify-space-between align-center mt-6">
      <h2 class="px-4">Scopet</h2>
      <v-btn
        color="primary"
        variant="text"
        prepend-icon="mdi-plus"
        size="small"
        @click="createScopeDialogOpen = true"
        >Lisää uusi</v-btn
      >
    </v-row>
    <v-sheet class="pb-2">
      <v-table v-if="scopePagedList">
        <thead>
          <tr>
            <th>Nimi</th>
            <th>Näyttönimi</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="scope in scopePagedList.items" :key="scope.id">
            <td>
              <router-link
                color="primary"
                :to="{ name: RouteNames.ScopeDetails, params: { id: scope.id } }"
                >{{ scope.name }}
              </router-link>
            </td>
            <td>
              {{ scope.displayName }}
            </td>
          </tr>
        </tbody>
      </v-table>

      <v-pagination
        v-if="scopePagedList && scopePagedList.totalPages > 1"
        :model-value="scopePagedList.pageNumber"
        :length="scopePagedList.totalPages"
        :density="'comfortable'"
        @update:model-value="(pageNumber) => scopePagedList.setPage(pageNumber)"
      />
    </v-sheet>
  </div>

  <create-role-dialog
    v-if="resourceServer"
    :open="createRoleDialogOpen"
    :resource-server="resourceServer"
    @cancel="() => (createRoleDialogOpen = false)"
    @created="onRoleCreated"
  />

  <create-scope-dialog
    v-if="resourceServer"
    :open="createScopeDialogOpen"
    :resource-server="resourceServer"
    @cancel="() => (createScopeDialogOpen = false)"
    @created="onScopeCreated"
  />

  <v-alert class="not-found" v-if="notFound" type="warning" variant="tonal">
    Resurssipalvelinta ei löytynyt.
  </v-alert>
</template>
