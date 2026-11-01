import type {
  PagedListDto,
  ScopeCreateRequest,
  ScopeDto,
  ScopePagedListRequest,
  ScopeUpdateRequest,
} from '@/api/generated'
import { httpGet, httpGetOrNull, httpPost, httpPut } from '@/api/httpClient'

export const getScopePagedList = (params: ScopePagedListRequest) =>
  httpGet<PagedListDto<ScopeDto>>('/scope', params)

export const getScopeById = (id: string) => httpGetOrNull<ScopeDto>(`/scope/${id}`)

export const createScope = (request: ScopeCreateRequest) => httpPost<string>('/scope', request)

export const updateScope = (request: ScopeUpdateRequest) => httpPut<void>('/scope', request)
