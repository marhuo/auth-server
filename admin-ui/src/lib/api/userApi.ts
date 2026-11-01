import { distinct, toMap } from '#lib/utils/collectionUtils.ts';
import type {
	AddUserRoleRequest,
	PagedListDto,
	RemoveUserRoleRequest,
	UserChangePasswordRequest,
	UserDto,
	UserInviteRequest,
	UserPagedListRequest,
	UserUpdateRequest
} from '#lib/api/generated/index.ts';
import { httpGet, httpGetOrNull, httpPost, httpPut } from '#lib/api/httpClient.ts';

export const getUserPagedList = (params: UserPagedListRequest) =>
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

// Returns the id of the invited user (an existing user if the email is already known).
export const inviteUser = (request: UserInviteRequest) => httpPost<string>('/user/invite', request);

// Sends a new invitation link to a user who has not set a password yet; earlier links stop working.
export const resendUserInvite = (id: string) => httpPost<void>(`/user/${id}/resend-invite`);

export const addUserRole = (request: AddUserRoleRequest) =>
	httpPost<void>('/user/role/add', request);

export const removeUserRole = (request: RemoveUserRoleRequest) =>
	httpPost<void>('/user/role/remove', request);

export const updateUser = (request: UserUpdateRequest) => httpPut<void>('/user', request);

export const changeUserPassword = (request: UserChangePasswordRequest) =>
	httpPost<void>('/user/change-password', request);
