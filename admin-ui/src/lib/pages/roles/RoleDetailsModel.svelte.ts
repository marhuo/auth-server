import type { ResourceServerDto } from '#lib/api/generated/index.ts';
import { getResourceServerById } from '#lib/api/resourceServerApi.ts';
import { deleteRole, getRoleById, updateRole } from '#lib/api/roleApi.ts';

export class RoleDetailsModel {
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

	private _name = $state('');
	get name() {
		return this._name;
	}

	private _description = $state('');
	get description() {
		return this._description;
	}

	private _resourceServerId = $state('');
	get resourceServerId() {
		return this._resourceServerId;
	}

	private _resourceServerName = $state('');
	get resourceServerName() {
		return this._resourceServerName;
	}

	private _isAuthServer = $state(false);
	get isAuthServer() {
		return this._isAuthServer;
	}

	private _createdAt = $state('');
	get createdAt() {
		return this._createdAt;
	}

	load = async () => {
		this._loading = true;
		try {
			const role = await getRoleById(this.id);
			if (!role) {
				this._notFound = true;
				return;
			}
			const resourceServer = await getResourceServerById(role.resourceServerId);

			this._name = role.name;
			this._description = role.description;
			this._resourceServerId = role.resourceServerId;
			this._resourceServerName = resourceServer?.displayName ?? '';
			this._isAuthServer = resourceServer?.isAuthServer ?? false;
			this._createdAt = role.createdAt;
			this._notFound = false;
		} finally {
			this._loading = false;
		}
	};

	private update(changes: { name?: string; description?: string; resourceServerId?: string }) {
		return updateRole({
			id: this.id,
			name: changes.name ?? this._name,
			description: changes.description ?? this._description,
			resourceServerId: changes.resourceServerId ?? this._resourceServerId
		});
	}

	changeName = async (name: string) => {
		await this.update({ name });
		this._name = name;
	};

	changeDescription = async (description: string) => {
		await this.update({ description });
		this._description = description;
	};

	changeResourceServer = async (resourceServer: ResourceServerDto | null) => {
		if (!resourceServer) return;
		await this.update({ resourceServerId: resourceServer.id });
		this._resourceServerId = resourceServer.id;
		this._resourceServerName = resourceServer.displayName;
	};

	delete = () => deleteRole(this.id);
}
