import type {
  PagedListDto,
  PagedListRequest,
  TenantCreateRequest,
  TenantDto,
  TenantSuspendRequest,
  TenantUpdateRequest,
} from '@/api/generated'
import { httpGet, httpGetOrNull, httpPost, httpPut } from '@/api/httpClient'

export const getTenantPagedList = (params: PagedListRequest) =>
  httpGet<PagedListDto<TenantDto>>('/tenant', params)

export const getTenantById = (id: string) => httpGetOrNull<TenantDto>(`/tenant/${id}`)

export const createTenant = (request: TenantCreateRequest) => httpPost<string>('/tenant', request)

export const updateTenant = (request: TenantUpdateRequest) => httpPut<void>('/tenant', request)

export const suspendTenant = (request: TenantSuspendRequest) =>
  httpPost<void>('/tenant/suspend', request)

export const unsuspendTenant = (id: string) => httpPost<void>(`/tenant/${id}/unsuspend`)
