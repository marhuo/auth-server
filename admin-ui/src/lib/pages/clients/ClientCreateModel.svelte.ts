import type { ClientDtoTypes, ScopeDto } from '#lib/api/generated/index.ts';
import { createClient } from '#lib/api/clientApi.ts';
import { getScopesSelectableForClient } from '#lib/api/scopeApi.ts';
import { clientTypeHasUsers, clientTypeNeedsSecret } from '#lib/models/clientTypes.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';
import { normalizeClientId } from '#lib/utils/normalizers.ts';

export class ClientCreateModel extends FormModelBase {
	private _type = $state<ClientDtoTypes>('PublicApplication');
	get type(): ClientDtoTypes {
		return this._type;
	}
	set type(value: string) {
		this._type = value as ClientDtoTypes;
		if (!this.hasUsers) {
			this.selectedScopes = this.selectedScopes.filter((s) => s.resourceServerId !== null);
		}
		this.reloadScopes();
	}

	private _targetResourceServerId = $state('');
	get targetResourceServerId() {
		return this._targetResourceServerId;
	}
	set targetResourceServerId(value: string) {
		this._targetResourceServerId = value;
		this.selectedScopes = this.selectedScopes.filter((s) => s.resourceServerId === null);
		this.reloadScopes();
	}

	private _clientId = $state('');
	get clientId() {
		return this._clientId;
	}
	set clientId(value: string) {
		this._clientId = normalizeClientId(value);
	}

	secret = $state('');
	displayName = $state('');
	tenantId = $state<string | null>(null);
	redirectUris = $state<string[]>([]);
	postLogoutRedirectUris = $state<string[]>([]);
	// The product's address that starts the sign-in (backend: Client.InitiateLoginUri). Optional.
	initiateLoginUri = $state('');
	selectedScopes = $state<ScopeDto[]>([]);

	get needsSecret() {
		return clientTypeNeedsSecret(this._type);
	}

	get hasUsers() {
		return clientTypeHasUsers(this._type);
	}

	private _selectableScopes = $state<ScopeDto[]>([]);
	get selectableScopes() {
		return this._selectableScopes;
	}

	private reloadScopes = async () => {
		const targetResourceServerId = this._targetResourceServerId;
		const type = this._type;

		if (!targetResourceServerId) {
			this._selectableScopes = [];
			return;
		}

		const scopes = await getScopesSelectableForClient(targetResourceServerId, type);

		if (this._targetResourceServerId !== targetResourceServerId || this._type !== type) return;

		this._selectableScopes = scopes;
	};

	submit = () =>
		this.run(
			{
				type: this._type,
				clientId: this._clientId,
				displayName: this.displayName,
				secret: this.needsSecret ? this.secret : null,
				targetResourceServerId: this._targetResourceServerId,
				tenantId: this.tenantId,
				redirectUris: this.hasUsers ? this.redirectUris.filter((u) => u.trim()) : [],
				postLogoutRedirectUris: this.hasUsers
					? this.postLogoutRedirectUris.filter((u) => u.trim())
					: [],
				initiateLoginUri: this.hasUsers ? this.initiateLoginUri.trim() || null : null,
				scopeIds: this.selectedScopes.map((s) => s.id)
			},
			createClient
		);
}
