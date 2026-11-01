import type { UserDtoStatuses } from '#lib/api/generated/index.ts';
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
	email: string;
	status: UserDtoStatuses;
	lockedUntil: string | null;
	// Locked by an admin.
	locked: boolean;
	// Automatic lockout (lockedUntil in the future) when the page was loaded.
	lockedOut: boolean;
	roles: UserListRole[];
}

// Users with their roles. Used by the users page (all users) and the tenant details page (the users
// of one tenant).
export class UserListModel extends PagedListBase<UserListItem> {
	// Null lists all users; otherwise only the users who have a role in this tenant.
	private readonly tenantId: string | null;

	constructor(tenantId: string | null) {
		super();
		this.tenantId = tenantId;
	}

	protected async fetch() {
		const page = await getUserPagedList({
			pageNumber: this.pageNumber,
			pageSize: this.pageSize,
			searchTerm: this.searchTerm || null,
			tenantId: this.tenantId
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

		const now = Date.now();

		const items: UserListItem[] = page.items.map((user) => ({
			id: user.id,
			email: user.email,
			status: user.status,
			lockedUntil: user.lockedUntil,
			locked: user.locked,
			lockedOut: user.lockedUntil !== null && Date.parse(user.lockedUntil) > now,
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
