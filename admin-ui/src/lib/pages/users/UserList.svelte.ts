import { getRolesByIds } from '#lib/api/roleApi.ts';
import { getTenantsByIds } from '#lib/api/tenantApi.ts';
import { getUserPagedList } from '#lib/api/userApi.ts';
import { PagedListBase } from '#lib/models/PagedList.svelte.ts';
import { distinct, toMap } from '#lib/utils/collectionUtils.ts';

export interface UserListRole {
	roleId: string;
	roleName: string;
	roleDescription: string;
	tenantId: string | null;
	tenantName: string | null;
}

export interface UserListItem {
	id: string;
	userName: string;
	email: string;
	roles: UserListRole[];
}

export class UserList extends PagedListBase<UserListItem> {
	protected async fetch() {
		const page = await getUserPagedList({
			pageNumber: this.pageNumber,
			pageSize: this.pageSize,
			searchTerm: this.searchTerm || null
		});

		const userRoles = page.items.flatMap((user) => user.userRoles);
		const roles = await getRolesByIds(distinct(userRoles.map((ur) => ur.roleId)));
		const tenants = await getTenantsByIds(
			distinct(userRoles.filter((ur) => ur.tenantId).map((ur) => ur.tenantId!))
		);

		const roleMap = toMap(
			roles,
			(r) => r.id,
			(r) => r
		);
		const tenantMap = toMap(
			tenants,
			(t) => t.id,
			(t) => t
		);

		const items: UserListItem[] = page.items.map((user) => ({
			id: user.id,
			userName: user.userName,
			email: user.email,
			roles: user.userRoles.map((ur) => ({
				roleId: ur.roleId,
				roleName: roleMap.get(ur.roleId)?.name ?? ur.roleId,
				roleDescription: roleMap.get(ur.roleId)?.description ?? '',
				tenantId: ur.tenantId,
				tenantName: ur.tenantId ? (tenantMap.get(ur.tenantId)?.name ?? null) : null
			}))
		}));

		return { ...page, items };
	}
}
