<script setup lang="ts">
import type { ResourceServerDto } from '@/api/generated'
import { getResourceServerPagedList } from '@/api/resourceServerApi'
import LinearLoading from '@/components/LinearLoading.vue'
import { usePagedList } from '@/composables/usePagedList'
import { RouteNames } from '@/router'

const pagedList = usePagedList<ResourceServerDto>({
  load: getResourceServerPagedList,
  pageSize: 10,
})
</script>

<template>
  <h1>Resurssipalvelimet</h1>

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
      :to="{ name: RouteNames.ResourceServerCreate }"
      >Lisää uusi</v-btn
    >
  </v-row>
  <v-sheet>
    <LinearLoading :loading="pagedList.loading" />
    <v-table v-if="pagedList">
      <thead>
        <tr>
          <th>Nimi</th>
          <th>Tunniste (audience)</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="rs in pagedList.items" :key="rs.id">
          <td>
            <router-link
              color="primary"
              :to="{ name: RouteNames.ResourceServerDetails, params: { id: rs.id } }"
              >{{ rs.displayName }}
            </router-link>
          </td>
          <td>{{ rs.audience }}</td>
        </tr>
      </tbody>
    </v-table>

    <v-pagination
      v-if="!pagedList.loading"
      :model-value="pagedList.pageNumber"
      :length="pagedList.totalPages"
      @update:model-value="(pageNumber) => pagedList.setPage(pageNumber)"
    />
  </v-sheet>
</template>
