// src/lib/api/roleApi.ts
import type { PagedListDto } from '#lib/api/generated/pagedListDto.ts';
import type { RoleCreateRequest } from '#lib/api/generated/roleCreateRequest.ts';
import type { RoleDto } from '#lib/api/generated/roleDto.ts';
import type { RolePagedListRequest } from '#lib/api/generated/rolePagedListRequest.ts';
import type { RoleUpdateRequest } from '#lib/api/generated/roleUpdateRequest.ts';
import { httpDelete, httpGet, httpGetOrNull, httpPost, httpPut } from '#lib/api/httpClient.ts';
import { distinct, toMap } from '#lib/utils/collectionUtils.ts';

export const getRolePagedList = (params: RolePagedListRequest) =>
	httpGet<PagedListDto<RoleDto>>('/role', params);

export const getRoleById = (id: string) => httpGetOrNull<RoleDto>(`/role/${id}`);

export const getRolesByIds = (ids: string[]) =>
	ids.length === 0 ? Promise.resolve<RoleDto[]>([]) : httpPost<RoleDto[]>('/role/by-ids', ids);

export const getRoleMapByIds = async (ids: string[]) => {
	const roles = await getRolesByIds(distinct(ids));
	return toMap(
		roles,
		(role) => role.id,
		(role) => role
	);
};

export const createRole = (request: RoleCreateRequest) => httpPost<string>('/role', request);

export const updateRole = (request: RoleUpdateRequest) => httpPut<void>('/role', request);

export const deleteRole = (id: string) => httpDelete(`/role/${id}`);
