import { distinct, toMap } from '#lib/utils/collectionUtils.ts';
import type {
	PagedListDto,
	PagedListRequest,
	TenantCreateRequest,
	TenantDto,
	TenantSuspendRequest,
	TenantUpdateRequest
} from './generated';
import { httpDelete, httpGet, httpGetOrNull, httpPost, httpPut } from './httpClient';

export const getTenantPagedList = (params: PagedListRequest) =>
	httpGet<PagedListDto<TenantDto>>('/tenant', params);

export const getTenantById = (id: string) => httpGetOrNull<TenantDto>(`/tenant/${id}`);

export const getTenantsByIds = (ids: string[]) =>
	ids.length === 0
		? Promise.resolve<TenantDto[]>([])
		: httpPost<TenantDto[]>('/tenant/by-ids', ids);

export const getTenantMapByIds = async (ids: string[]) => {
	const tenants = await getTenantsByIds(distinct(ids));
	return toMap(
		tenants,
		(tenant) => tenant.id,
		(tenant) => tenant
	);
};

export const createTenant = (request: TenantCreateRequest) => httpPost<string>('/tenant', request);

export const updateTenant = (request: TenantUpdateRequest) => httpPut<void>('/tenant', request);

export const suspendTenant = (request: TenantSuspendRequest) =>
	httpPost<void>('/tenant/suspend', request);

export const unsuspendTenant = (id: string) => httpPost<void>(`/tenant/${id}/unsuspend`);

export const deleteTenant = (id: string) => httpDelete(`/scope/${id}`);
