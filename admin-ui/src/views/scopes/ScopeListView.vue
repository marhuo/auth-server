<script setup lang="ts">
import type { ResourceServerDto, ScopeDto } from '@/api/generated'
import { getResourceServersByIds } from '@/api/resourceServerApi'
import { getScopePagedList } from '@/api/scopeApi'
import { usePagedList } from '@/composables/usePagedList'
import { RouteNames } from '@/router'
import { ref, watch } from 'vue'

const pagedList = usePagedList<ScopeDto>({
  load: (request) => getScopePagedList({ ...request, resourceServerId: null }),
  pageSize: 10,
})
const resourceServers = ref(new Map<string, ResourceServerDto>())

watch(pagedList, async (list) => {
  if (!list) return
  const ids = [
    ...new Set(
      list.items
        .filter((scope) => !!scope.resourceServerId)
        .map((scope) => scope.resourceServerId!),
    ),
  ]
  const servers = await getResourceServersByIds(ids)
  resourceServers.value = new Map(servers.map((rs) => [rs.id, rs]))
})
</script>

<template>
  <h1>Scopet</h1>

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
      :to="{ name: RouteNames.ScopeCreate }"
      >Lisää uusi</v-btn
    >
  </v-row>

  <v-table>
    <thead>
      <tr>
        <th>Nimi</th>
        <th>Näytettävänimi</th>
        <th>Resurssipalvelin</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="scope in pagedList?.items" :key="scope.id">
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

        <td>
          <router-link
            color="primary"
            v-if="scope.resourceServerId"
            :to="{
              name: RouteNames.ResourceServerDetails,
              params: { id: scope.resourceServerId },
            }"
            >{{ resourceServers.get(scope.resourceServerId)?.displayName }}
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
