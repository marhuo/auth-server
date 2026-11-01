import { distinct, toMap } from '#lib/utils/collectionUtils.ts';
import type {
	ClientDtoTypes,
	PagedListDto,
	ScopeCreateRequest,
	ScopeDto,
	ScopePagedListRequest,
	ScopeUpdateRequest
} from './generated';
import { httpDelete, httpGet, httpGetOrNull, httpPost, httpPut } from './httpClient';

export const getScopePagedList = (params: ScopePagedListRequest) =>
	httpGet<PagedListDto<ScopeDto>>('/scope', params);

export const getScopeById = (id: string) => httpGetOrNull<ScopeDto>(`/scope/${id}`);

export const getScopesByIds = (ids: string[]) =>
	ids.length === 0 ? Promise.resolve<ScopeDto[]>([]) : httpPost<ScopeDto[]>('/scope/by-ids', ids);

export const getScopeMapByIds = async (ids: string[]) => {
	const scopes = await getScopesByIds(distinct(ids));
	return toMap(
		scopes,
		(scope) => scope.id,
		(scope) => scope
	);
};

export const createScope = (request: ScopeCreateRequest) => httpPost<string>('/scope', request);

export const updateScope = (request: ScopeUpdateRequest) => httpPut<void>('/scope', request);

export const deleteScope = (id: string) => httpDelete(`/scope/${id}`);

export const getScopesSelectableForClient = (
	resourceServerId: string,
	clientType: ClientDtoTypes
) => httpGet<ScopeDto[]>(`/scope/selectable-for-client/${resourceServerId}`, { clientType });
