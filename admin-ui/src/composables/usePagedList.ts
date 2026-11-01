// src/composables/usePagedList.ts
import type { PagedListDto, PagedListRequest } from '@/api/generated'
import { computed, reactive, ref, watch, type Reactive } from 'vue'
import { useRoute, useRouter } from 'vue-router'

export interface PagedListOptions<TItem> {
  load: (request: PagedListRequest) => Promise<PagedListDto<TItem>>
  pageSize?: number
  pageKey?: string
  searchKey?: string
}

export interface PagedList<TItem> {
  items: TItem[]
  totalCount: number
  totalPages: number
  hasPrevious: boolean
  hasNext: boolean
  loading: boolean
  pageNumber: number
  searchTerm: string
  setPage: (page: number) => void
  setSearch: (search: string | null) => void
  reload: () => Promise<void>
}

export function usePagedList<TItem>(options: PagedListOptions<TItem>): Reactive<PagedList<TItem>> {
  const { load, pageSize = 10, pageKey = 'page', searchKey = 'search' } = options

  const route = useRoute()
  const router = useRouter()

  const pageNumber = computed(() => Number(route.query[pageKey]) || 1)
  const searchTerm = computed(() => String(route.query[searchKey] ?? ''))

  const pagedList = ref<PagedListDto<TItem> | null>(null)
  const loading = ref(false)

  const items = computed(() => pagedList.value?.items ?? [])
  const totalCount = computed(() => pagedList.value?.totalCount ?? 0)
  const totalPages = computed(() => pagedList.value?.totalPages || 1)
  const hasPrevious = computed(() => pagedList.value?.hasPrevious ?? false)
  const hasNext = computed(() => pagedList.value?.hasNext ?? false)

  watch([pageNumber, searchTerm], reload, { immediate: true })

  async function reload() {
    loading.value = true
    try {
      pagedList.value = await load({
        pageNumber: pageNumber.value,
        pageSize,
        searchTerm: searchTerm.value || null,
      })
    } finally {
      loading.value = false
    }
  }

  function updateQuery(changes: Record<string, string | number | undefined>) {
    router.replace({ query: { ...route.query, ...changes } })
  }

  function setPage(page: number) {
    updateQuery({ [pageKey]: page })
  }

  function setSearch(search: string | null) {
    updateQuery({ [searchKey]: search || undefined, [pageKey]: undefined })
  }

  return reactive({
    items,
    totalCount,
    totalPages,
    hasPrevious,
    hasNext,
    loading,
    pageNumber,
    searchTerm,
    setPage,
    setSearch,
    reload,
  }) as Reactive<PagedList<TItem>>
}
