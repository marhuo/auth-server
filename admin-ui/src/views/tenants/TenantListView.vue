<script setup lang="ts">
import type { TenantDto } from '@/api/generated'
import { getTenantPagedList } from '@/api/tenantApi'
import LinearLoading from '@/components/LinearLoading.vue'
import { usePagedList } from '@/composables/usePagedList'
import { RouteNames } from '@/router'

const pagedList = usePagedList<TenantDto>({
  load: getTenantPagedList,
  pageSize: 10,
})
</script>

<template>
  <h1>Asiakkaat</h1>

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
      :to="{ name: RouteNames.TenantCreate }"
      >Lisää uusi</v-btn
    >
  </v-row>
  <LinearLoading :loading="pagedList.loading" />
  <v-table v-if="pagedList">
    <thead>
      <tr>
        <th>Nimi</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="tenant in pagedList.items" :key="tenant.id">
        <td>
          <router-link
            color="primary"
            :to="{ name: RouteNames.TenantDetails, params: { id: tenant.id } }"
            >{{ tenant.name }}
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
