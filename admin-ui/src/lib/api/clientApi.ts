// src/lib/api/clientApi.ts
import type { ClientAddPostLogoutRedirectUriRequest } from '#lib/api/generated/clientAddPostLogoutRedirectUriRequest.ts';
import type { ClientAddRedirectUriRequest } from '#lib/api/generated/clientAddRedirectUriRequest.ts';
import type { ClientAddScopeRequest } from '#lib/api/generated/clientAddScopeRequest.ts';
import type { ClientChangeSecretRequest } from '#lib/api/generated/clientChangeSecretRequest.ts';
import type { ClientCreateRequest } from '#lib/api/generated/clientCreateRequest.ts';
import type { ClientDto } from '#lib/api/generated/clientDto.ts';
import type { ClientRemovePostLogoutRedirectUriRequest } from '#lib/api/generated/clientRemovePostLogoutRedirectUriRequest.ts';
import type { ClientRemoveRedirectUriRequest } from '#lib/api/generated/clientRemoveRedirectUriRequest.ts';
import type { ClientRemoveScopeRequest } from '#lib/api/generated/clientRemoveScopeRequest.ts';
import type { ClientUpdateRequest } from '#lib/api/generated/clientUpdateRequest.ts';
import type { PagedListDto } from '#lib/api/generated/pagedListDto.ts';
import type { PagedListRequest } from '#lib/api/generated/pagedListRequest.ts';
import { httpDelete, httpGet, httpGetOrNull, httpPost, httpPut } from '#lib/api/httpClient.ts';
import { distinct, toMap } from '#lib/utils/collectionUtils.ts';

export const getClientPagedList = (params: PagedListRequest) =>
	httpGet<PagedListDto<ClientDto>>('/client', params);

export const getClientById = (id: string) => httpGetOrNull<ClientDto>(`/client/${id}`);

export const getClientsByIds = (ids: string[]) =>
	ids.length === 0
		? Promise.resolve<ClientDto[]>([])
		: httpPost<ClientDto[]>('/client/by-ids', ids);

export const getClientMapByIds = async (ids: string[]) => {
	const clients = await getClientsByIds(distinct(ids));
	return toMap(
		clients,
		(client) => client.id,
		(client) => client
	);
};

export const createClient = (request: ClientCreateRequest) => httpPost<string>('/client', request);

export const updateClient = (request: ClientUpdateRequest) => httpPut<void>('/client', request);

export const deleteClient = (id: string) => httpDelete(`/client/${id}`);

export const deactivateClient = (id: string) => httpPost<void>(`/client/${id}/deactivate`);

export const changeClientSecret = (request: ClientChangeSecretRequest) =>
	httpPost<void>('/client/change-secret', request);

export const addClientScope = (request: ClientAddScopeRequest) =>
	httpPost<void>('/client/scope/add', request);

export const removeClientScope = (request: ClientRemoveScopeRequest) =>
	httpPost<void>('/client/scope/remove', request);

export const addClientRedirectUri = (request: ClientAddRedirectUriRequest) =>
	httpPost<void>('/client/redirect-uri/add', request);

export const removeClientRedirectUri = (request: ClientRemoveRedirectUriRequest) =>
	httpPost<void>('/client/redirect-uri/remove', request);

export const addClientPostLogoutRedirectUri = (request: ClientAddPostLogoutRedirectUriRequest) =>
	httpPost<void>('/client/post-logout-redirect-uri/add', request);

export const removeClientPostLogoutRedirectUri = (
	request: ClientRemovePostLogoutRedirectUriRequest
) => httpPost<void>('/client/post-logout-redirect-uri/remove', request);
