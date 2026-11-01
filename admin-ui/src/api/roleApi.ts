import type {
  PagedListDto,
  RoleCreateRequest,
  RoleDto,
  RolePagedListRequest,
  RoleUpdateRequest,
} from '@/api/generated'
import { httpGet, httpGetOrNull, httpPost, httpPut } from '@/api/httpClient'

export const getRolePagedList = (params: RolePagedListRequest) =>
  httpGet<PagedListDto<RoleDto>>('/role', params)

export const getRoleById = (id: string) => httpGetOrNull<RoleDto>(`/role/${id}`)

export const createRole = (request: RoleCreateRequest) => httpPost<string>('/role', request)

export const updateRole = (request: RoleUpdateRequest) => httpPut<string>('/role', request)
