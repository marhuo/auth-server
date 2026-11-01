import type { ResourceServerDto, RoleDto, TenantDto } from '#lib/api/generated/index.ts';
import { getResourceServerById } from '#lib/api/resourceServerApi.ts';
import { addUserRole } from '#lib/api/userApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';

export class AddRoleModel extends FormModelBase {
	private _role = $state<RoleDto | null>(null);
	get role() {
		return this._role;
	}
	set role(value: RoleDto | null) {
		this._role = value;
		this.tenant = null;
		this.loadResourceServer();
	}

	tenant = $state<TenantDto | null>(null);

	private _resourceServer = $state<ResourceServerDto | null>(null);
	get resourceServer() {
		return this._resourceServer;
	}

	get tenantDisabled() {
		return !this._role || !this._resourceServer || this._resourceServer.isAuthServer;
	}

	get canSubmit() {
		if (this.submitting || !this._role) return false;
		if (this._resourceServer?.isAuthServer) return this.tenant === null;
		return this.tenant !== null;
	}

	private loadResourceServer = async () => {
		const role = this._role;
		if (!role) {
			this._resourceServer = null;
			return;
		}
		const resourceServer = await getResourceServerById(role.resourceServerId);
		if (this._role === role) this._resourceServer = resourceServer;
	};

	submit = (userId: string) =>
		this.run(
			{ userId, roleId: this._role?.id ?? '', tenantId: this.tenant?.id ?? null },
			addUserRole
		);
}
