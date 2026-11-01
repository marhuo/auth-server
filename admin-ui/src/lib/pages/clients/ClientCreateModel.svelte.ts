import type { ClientDtoTypes, ScopeDto } from '#lib/api/generated/index.ts';
import { createClient } from '#lib/api/clientApi.ts';
import { getScopesSelectableForClient } from '#lib/api/scopeApi.ts';
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

	private _resourceServerId = $state('');
	get resourceServerId() {
		return this._resourceServerId;
	}
	set resourceServerId(value: string) {
		this._resourceServerId = value;
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
	selectedScopes = $state<ScopeDto[]>([]);

	get needsSecret() {
		return this._type === 'ConfidentialApplication' || this._type === 'ConfidentialMachine';
	}

	get hasUsers() {
		return this._type !== 'ConfidentialMachine';
	}

	private _selectableScopes = $state<ScopeDto[]>([]);
	get selectableScopes() {
		return this._selectableScopes;
	}

	private reloadScopes = async () => {
		const resourceServerId = this._resourceServerId;
		const type = this._type;

		if (!resourceServerId) {
			this._selectableScopes = [];
			return;
		}

		const scopes = await getScopesSelectableForClient(resourceServerId, type);

		if (this._resourceServerId !== resourceServerId || this._type !== type) return;

		this._selectableScopes = scopes;
	};

	submit = () =>
		this.run(
			{
				type: this._type,
				clientId: this._clientId,
				displayName: this.displayName,
				secret: this.needsSecret ? this.secret : null,
				resourceServerId: this._resourceServerId,
				tenantId: this.tenantId,
				redirectUris: this.hasUsers ? this.redirectUris.filter((u) => u.trim()) : [],
				postLogoutRedirectUris: this.hasUsers
					? this.postLogoutRedirectUris.filter((u) => u.trim())
					: [],
				scopeIds: this.selectedScopes.map((s) => s.id)
			},
			createClient
		);
}
