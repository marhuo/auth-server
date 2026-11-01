// src/stores/resourceServers.ts
import type {
  ResourceServerCreateRequest,
  ResourceServerDto,
  ResourceServerUpdateRequest,
} from '@/api/generated'
import {
  createResourceServer,
  getAllResourceServers,
  getResourceServerById,
  updateResourceServer,
} from '@/api/resourceServerApi'
import { computed, reactive, ref } from 'vue'

const resourceServers = ref<ResourceServerDto[]>([])
const loading = ref(false)
const pageNumber = ref(1)
const pageSize = 10
const searchTerm = ref('')
let loaded = false

const filtered = computed(() => {
  const term = searchTerm.value.toLocaleLowerCase()
  return resourceServers.value
    .filter(
      (rs) =>
        !term ||
        rs.displayName.toLocaleLowerCase().includes(term) ||
        rs.audience.toLocaleLowerCase().includes(term),
    )
    .sort((a, b) => a.displayName.localeCompare(b.displayName))
})

const pageItems = computed(() => {
  const start = (pageNumber.value - 1) * pageSize
  return filtered.value.slice(start, start + pageSize)
})

const pageCount = computed(() => Math.ceil(filtered.value.length / pageSize))

async function load() {
  loading.value = true
  try {
    resourceServers.value = await getAllResourceServers()
    loaded = true
  } finally {
    loading.value = false
  }
}

async function create(request: ResourceServerCreateRequest) {
  const id = await createResourceServer(request)
  const created = await getResourceServerById(id)
  if (created) resourceServers.value.push(created)
  return id
}

async function update(request: ResourceServerUpdateRequest) {
  await updateResourceServer(request)
  const updated = await getResourceServerById(request.id)
  if (updated)
    resourceServers.value = resourceServers.value.map((rs) => (rs.id === request.id ? updated : rs))
}

function setPageNumber(num: number) {
  pageNumber.value = num
}

function setSearchTerm(term: string) {
  searchTerm.value = term
  pageNumber.value = 1
}

export function useResourceServers() {
  if (!loaded && !loading.value) {
    load()
  }
  return reactive({
    resourceServers,
    loading,
    pageItems,
    pageCount,
    pageNumber,
    searchTerm,
    create,
    update,
    setPageNumber,
    setSearchTerm,
  })
}
