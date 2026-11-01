<script setup lang="ts">
import type { ResourceServerDto, RoleDto } from '@/api/generated'
import { getResourceServersByIds } from '@/api/resourceServerApi'
import { getRolePagedList } from '@/api/roleApi'
import { usePagedList } from '@/composables/usePagedList'
import { RouteNames } from '@/router'
import { ref, watch } from 'vue'

const pagedList = usePagedList<RoleDto>({
  load: (request) => getRolePagedList({ ...request, resourceServerId: null }),
  pageSize: 10,
})

const resourceServers = ref(new Map<string, ResourceServerDto>())

watch(pagedList, async (list) => {
  if (!list) return
  const ids = [...new Set(list.items.map((role) => role.resourceServerId))]
  const servers = await getResourceServersByIds(ids)
  resourceServers.value = new Map(servers.map((rs) => [rs.id, rs]))
})
</script>

<template>
  <h1>Roolit</h1>

  <v-row class="justify-space-between">
    <v-text-field
      :model-value="pagedList.searchTerm"
      label="Hae"
      append-inner-icon="mdi-magnify"
      style="max-width: 400px"
      variant="solo"
      clearable
      @update:model-value="(v) => pagedList.setSearch(v)"
    />
    <v-btn
      color="primary"
      variant="text"
      prepend-icon="mdi-plus"
      size="small"
      :to="{ name: RouteNames.RoleCreate }"
      >Lisää uusi</v-btn
    >
  </v-row>

  <v-table>
    <thead>
      <tr>
        <th>Nimi</th>
        <th>Kuvaus</th>
        <th>Resurssipalvelin</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="role in pagedList.items" :key="role.id">
        <td>
          <router-link
            color="primary"
            :to="{ name: RouteNames.RoleDetails, params: { id: role.id } }"
            >{{ role.name }}
          </router-link>
        </td>
        <td>
          {{ role.description }}
        </td>

        <td>
          <router-link
            color="primary"
            :to="{
              name: RouteNames.ResourceServerDetails,
              params: { id: role.resourceServerId },
            }"
            >{{ resourceServers.get(role.resourceServerId)?.displayName }}
          </router-link>
        </td>
      </tr>
    </tbody>
  </v-table>
  <v-pagination
    v-if="!pagedList.loading"
    :model-value="pagedList.pageNumber"
    :length="pagedList.totalPages"
    @update:model-value="(pageNumber) => pagedList.setPage(pageNumber)"
  />
</template>
