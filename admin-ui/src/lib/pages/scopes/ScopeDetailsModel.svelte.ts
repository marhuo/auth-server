import { getResourceServerById } from '#lib/api/resourceServerApi.ts';
import { deleteScope, getScopeById, updateScope } from '#lib/api/scopeApi.ts';

export class ScopeDetailsModel {
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

	private _displayName = $state('');
	get displayName() {
		return this._displayName;
	}

	private _resourceServerId = $state<string | null>(null);
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

	private _isStandard = $state(false);
	get isStandard() {
		return this._isStandard;
	}

	private _createdAt = $state('');
	get createdAt() {
		return this._createdAt;
	}

	get isGlobal() {
		return this._resourceServerId === null;
	}

	load = async () => {
		this._loading = true;
		try {
			const scope = await getScopeById(this.id);
			if (!scope) {
				this._notFound = true;
				return;
			}
			const resourceServer = scope.resourceServerId
				? await getResourceServerById(scope.resourceServerId)
				: null;

			this._name = scope.name;
			this._displayName = scope.displayName;
			this._resourceServerId = scope.resourceServerId;
			this._resourceServerName = resourceServer?.displayName ?? '';
			this._isAuthServer = resourceServer?.isAuthServer ?? false;
			this._isStandard = scope.isStandard;
			this._createdAt = scope.createdAt;
			this._notFound = false;
		} finally {
			this._loading = false;
		}
	};

	private update(changes: { name?: string; displayName?: string }) {
		return updateScope({
			id: this.id,
			name: changes.name ?? this._name,
			displayName: changes.displayName ?? this._displayName,
			resourceServerId: this._resourceServerId
		});
	}

	changeName = async (name: string) => {
		await this.update({ name });
		this._name = name;
	};

	changeDisplayName = async (displayName: string) => {
		await this.update({ displayName });
		this._displayName = displayName;
	};

	delete = () => deleteScope(this.id);
}
