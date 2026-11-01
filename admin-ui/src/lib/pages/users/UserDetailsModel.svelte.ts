import type { UserDtoStatuses } from '#lib/api/generated/index.ts';
import { getResourceServersByIds } from '#lib/api/resourceServerApi.ts';
import { getRolesByIds } from '#lib/api/roleApi.ts';
import { getTenantsByIds } from '#lib/api/tenantApi.ts';
import {
	changeUserPassword,
	getUserById,
	removeUserRole,
	resendUserInvite,
	updateUser
} from '#lib/api/userApi.ts';
import { distinct, toMap } from '#lib/utils/collectionUtils.ts';

export interface UserRoleDetails {
	roleId: string;
	roleName: string;
	roleDescription: string;
	resourceServerId: string;
	resourceServerName: string;
	tenantId: string | null;
	tenantName: string | null;
}

export class UserDetailsModel {
	readonly id: string;

	constructor(id: string) {
		this.id = id;
	}

	private _loading = $state(true);
	get loading() {
		return this._loading;
	}

	private _notFound = $state(false);
	get notFound() {
		return this._notFound;
	}

	private _email = $state('');
	get email() {
		return this._email;
	}

	private _hasPassword = $state(false);
	get hasPassword() {
		return this._hasPassword;
	}

	private _status = $state<UserDtoStatuses>('Invited');
	get status() {
		return this._status;
	}

	// Locked by an admin.
	private _locked = $state(false);
	get locked() {
		return this._locked;
	}

	// Automatic lockout after too many failed logins; read-only here.
	private _lockedUntil = $state<string | null>(null);
	get lockedUntil() {
		return this._lockedUntil;
	}

	// Whether the automatic lockout was in force when the user was loaded.
	get lockedOut() {
		return this._lockedUntil !== null && Date.parse(this._lockedUntil) > this._loadedAt;
	}

	private _loadedAt = 0;

	private _createdAt = $state('');
	get createdAt() {
		return this._createdAt;
	}

	private _userRoles = $state<UserRoleDetails[]>([]);
	get userRoles() {
		return this._userRoles;
	}

	load = async () => {
		this._loading = true;
		try {
			const user = await getUserById(this.id);
			if (!user) {
				this._notFound = true;
				return;
			}

			const roles = await getRolesByIds(distinct(user.userRoles.map((ur) => ur.roleId)));
			const resourceServers = await getResourceServersByIds(
				distinct(roles.map((r) => r.resourceServerId))
			);
			const tenants = await getTenantsByIds(
				distinct(user.userRoles.filter((ur) => ur.tenantId).map((ur) => ur.tenantId!))
			);

			const roleMap = toMap(
				roles,
				(r) => r.id,
				(r) => r
			);
			const resourceServerMap = toMap(
				resourceServers,
				(rs) => rs.id,
				(rs) => rs
			);
			const tenantMap = toMap(
				tenants,
				(t) => t.id,
				(t) => t
			);

			this._email = user.email;
			this._lockedUntil = user.lockedUntil;
			this._locked = user.locked;
			this._loadedAt = Date.now();
			this._hasPassword = user.hasPassword;
			this._status = user.status;
			this._createdAt = user.createdAt;
			this._userRoles = user.userRoles.map((ur) => {
				const role = roleMap.get(ur.roleId);
				const resourceServer = role ? resourceServerMap.get(role.resourceServerId) : undefined;
				const tenant = ur.tenantId ? tenantMap.get(ur.tenantId) : undefined;
				return {
					roleId: ur.roleId,
					roleName: role?.name ?? ur.roleId,
					roleDescription: role?.description ?? '',
					resourceServerId: role?.resourceServerId ?? '',
					resourceServerName: resourceServer?.displayName ?? '',
					tenantId: ur.tenantId,
					tenantName: tenant?.name ?? null
				};
			});
			this._notFound = false;
		} finally {
			this._loading = false;
		}
	};

	private update(changes: { email?: string; locked?: boolean }) {
		return updateUser({
			id: this.id,
			email: changes.email ?? this._email,
			locked: changes.locked ?? this._locked
		});
	}

	changePassword = async (password: string) => {
		await changeUserPassword({ id: this.id, newPassword: password });
		this._hasPassword = true;
		// Setting the password also accepts the invitation (backend: User.Status).
		this._status = 'Active';
	};

	private _resendingInvite = $state(false);
	get resendingInvite() {
		return this._resendingInvite;
	}

	// Set when the invitation has been sent again on this page.
	private _inviteResent = $state(false);
	get inviteResent() {
		return this._inviteResent;
	}

	resendInvite = async () => {
		this._resendingInvite = true;
		try {
			await resendUserInvite(this.id);
			this._inviteResent = true;
		} finally {
			this._resendingInvite = false;
		}
	};

	changeEmail = async (email: string) => {
		await this.update({ email });
		this._email = email;
	};

	// Unlocking also ends the automatic lockout (backend: User.SetLocked).
	changeLocked = async (locked: boolean) => {
		await this.update({ locked });
		this._locked = locked;
		if (!locked) this._lockedUntil = null;
	};

	removeRole = async (roleId: string, tenantId: string | null) => {
		await removeUserRole({ userId: this.id, roleId, tenantId });
		this._userRoles = this._userRoles.filter(
			(ur) => !(ur.roleId === roleId && ur.tenantId === tenantId)
		);
	};
}
