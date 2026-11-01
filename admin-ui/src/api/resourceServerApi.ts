import type {
  PagedListDto,
  PagedListRequest,
  ResourceServerCreateRequest,
  ResourceServerDto,
  ResourceServerUpdateRequest,
} from '@/api/generated'
import { httpGet, httpGetOrNull, httpPost, httpPut } from '@/api/httpClient'

export const getResourceServerPagedList = (params: PagedListRequest) =>
  httpGet<PagedListDto<ResourceServerDto>>('/resource-server', params)

export const getAllResourceServers = () => httpGet<ResourceServerDto[]>('/resource-server/all')

export const getResourceServersByIds = (ids: string[]) =>
  httpGet<ResourceServerDto[]>('/resource-server/by-ids', { ids })

export const getResourceServerById = (id: string) =>
  httpGetOrNull<ResourceServerDto>(`/resource-server/${id}`)

export const createResourceServer = (request: ResourceServerCreateRequest) =>
  httpPost<string>('/resource-server', request)

export const updateResourceServer = (request: ResourceServerUpdateRequest) =>
  httpPut<void>('/resource-server', request)
