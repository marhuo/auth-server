// src/lib/api/resourceServerApi.ts
import { distinct, toMap } from '#lib/utils/collectionUtils.ts';
import type {
	PagedListDto,
	PagedListRequest,
	ResourceServerCreateRequest,
	ResourceServerDto,
	ResourceServerUpdateRequest
} from './generated';
import { httpDelete, httpGet, httpGetOrNull, httpPost, httpPut } from './httpClient';

export const getResourceServerPagedList = (params: PagedListRequest) =>
	httpGet<PagedListDto<ResourceServerDto>>('/resource-server', params);

export const getAllResourceServers = () => httpGet<ResourceServerDto[]>('/resource-server/all');

export const getResourceServersByIds = (ids: string[]) =>
	ids.length === 0
		? Promise.resolve<ResourceServerDto[]>([])
		: httpPost<ResourceServerDto[]>('/resource-server/by-ids', ids);

export const getResourceServerMapByIds = async (ids: string[]) => {
	const resourceServers = await getResourceServersByIds(distinct(ids));
	return toMap(
		resourceServers,
		(rs) => rs.id,
		(rs) => rs
	);
};

export const getResourceServerById = (id: string) =>
	httpGetOrNull<ResourceServerDto>(`/resource-server/${id}`);

export const createResourceServer = (request: ResourceServerCreateRequest) =>
	httpPost<string>('/resource-server', request);

export const updateResourceServer = (request: ResourceServerUpdateRequest) =>
	httpPut<void>('/resource-server', request);

export const deleteResourceServer = (id: string) => httpDelete(`/resource-server/${id}`);
