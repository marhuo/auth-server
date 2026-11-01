import { distinct, toMap } from '#lib/utils/collectionUtils.ts';
import type {
	AddUserRoleRequest,
	PagedListDto,
	PagedListRequest,
	RemoveUserRoleRequest,
	UserChangePasswordRequest,
	UserCreateRequest,
	UserDto,
	UserUpdateRequest
} from './generated';
import { httpDelete, httpGet, httpGetOrNull, httpPost, httpPut } from './httpClient';

export const getUserPagedList = (params: PagedListRequest) =>
	httpGet<PagedListDto<UserDto>>('/user', params);

export const getUserById = (id: string) => httpGetOrNull<UserDto>(`/user/${id}`);

export const getUsersByIds = (ids: string[]) =>
	ids.length === 0 ? Promise.resolve<UserDto[]>([]) : httpPost<UserDto[]>('/user/by-ids', ids);

export const getUserMapByIds = async (ids: string[]) => {
	const users = await getUsersByIds(distinct(ids));
	return toMap(
		users,
		(user) => user.id,
		(user) => user
	);
};

export const createUser = (request: UserCreateRequest) => httpPost<string>('/user', request);

export const addUserRole = (request: AddUserRoleRequest) =>
	httpPost<void>('/user/role/add', request);

export const removeUserRole = (request: RemoveUserRoleRequest) =>
	httpPost<void>('/user/role/remove', request);

export const updateUser = (request: UserUpdateRequest) => httpPut<void>('/user', request);

export const changeUserPassword = (request: UserChangePasswordRequest) =>
	httpPost<void>('/user/change-password', request);

export const deleteUser = (id: string) => httpDelete(`/user/${id}`);
