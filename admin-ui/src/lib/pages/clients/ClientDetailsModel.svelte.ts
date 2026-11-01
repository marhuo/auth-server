import type { ClientDtoTypes, ScopeDto } from '#lib/api/generated/index.ts';
import {
	addClientPostLogoutRedirectUri,
	addClientRedirectUri,
	addClientScope,
	getClientById,
	removeClientPostLogoutRedirectUri,
	removeClientRedirectUri,
	removeClientScope,
	updateClient
} from '#lib/api/clientApi.ts';
import { getResourceServerById } from '#lib/api/resourceServerApi.ts';
import { getScopesByIds } from '#lib/api/scopeApi.ts';
import { getTenantById } from '#lib/api/tenantApi.ts';
import {
	clientTypeDescription,
	clientTypeHasUsers,
	clientTypeLabel
} from '#lib/models/clientTypes.ts';

export class ClientDetailsModel {
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

	private _hasUsers = $state(false);
	get hasUsers() {
		return this._hasUsers;
	}

	private _type = $state<ClientDtoTypes>('PublicApplication');
	get type() {
		return this._type;
	}

	private _typeLabel = $state('');
	get typeLabel() {
		return this._typeLabel;
	}

	private _typeDescription = $state('');
	get typeDescription() {
		return this._typeDescription;
	}

	private _targetResourceServerId = $state('');
	get targetResourceServerId() {
		return this._targetResourceServerId;
	}

	private _targetResourceServerName = $state('');
	get targetResourceServerName() {
		return this._targetResourceServerName;
	}

	private _tenantId = $state<string | null>(null);
	get tenantId() {
		return this._tenantId;
	}

	// The product's address that starts the sign-in (backend: Client.InitiateLoginUri).
	private _initiateLoginUri = $state<string | null>(null);
	get initiateLoginUri() {
		return this._initiateLoginUri;
	}

	private _tenantName = $state<string | null>(null);
	get tenantName() {
		return this._tenantName;
	}

	private _createdAt = $state('');
	get createdAt() {
		return this._createdAt;
	}

	private _allowedGrantTypes = $state<string[]>([]);
	get allowedGrantTypes() {
		return this._allowedGrantTypes;
	}

	private _accessTokenLifetimeSeconds = $state(0);
	get accessTokenLifetimeSeconds() {
		return this._accessTokenLifetimeSeconds;
	}

	private _idTokenLifetimeSeconds = $state<number | null>(null);
	get idTokenLifetimeSeconds() {
		return this._idTokenLifetimeSeconds;
	}

	private _refreshTokenLifetimeSeconds = $state<number | null>(null);
	get refreshTokenLifetimeSeconds() {
		return this._refreshTokenLifetimeSeconds;
	}

	private _clientId = $state('');
	get clientId() {
		return this._clientId;
	}

	private _displayName = $state('');
	get displayName() {
		return this._displayName;
	}

	private _redirectUris = $state<string[]>([]);
	get redirectUris() {
		return this._redirectUris;
	}

	private _postLogoutRedirectUris = $state<string[]>([]);
	get postLogoutRedirectUris() {
		return this._postLogoutRedirectUris;
	}

	private _scopes = $state<ScopeDto[]>([]);
	get scopes() {
		return this._scopes;
	}
	load = async (): Promise<void> => {
		this._loading = true;
		try {
			const client = await getClientById(this.id);
			if (!client) {
				this._notFound = true;
				return;
			}

			const resourceServer = await getResourceServerById(client.targetResourceServerId);
			const tenant = client.tenantId ? await getTenantById(client.tenantId) : null;
			const scopes = await getScopesByIds(client.scopeIds);

			this._type = client.type;
			this._typeLabel = clientTypeLabel(client.type);
			this._typeDescription = clientTypeDescription(client.type);
			this._hasUsers = clientTypeHasUsers(client.type);
			this._clientId = client.clientId;
			this._displayName = client.displayName;
			this._targetResourceServerId = client.targetResourceServerId;
			this._targetResourceServerName = resourceServer?.displayName ?? '';
			this._tenantId = client.tenantId;
			this._tenantName = tenant?.name ?? null;
			this._initiateLoginUri = client.initiateLoginUri;
			this._createdAt = client.createdAt;
			this._redirectUris = client.redirectUris;
			this._postLogoutRedirectUris = client.postLogoutRedirectUris;
			this._allowedGrantTypes = client.allowedGrantTypes;
			this._accessTokenLifetimeSeconds = client.accessTokenLifetimeSeconds;
			this._idTokenLifetimeSeconds = client.idTokenLifetimeSeconds;
			this._refreshTokenLifetimeSeconds = client.refreshTokenLifetimeSeconds;
			this._scopes = scopes;
			this._notFound = false;
		} finally {
			this._loading = false;
		}
	};

	// The update request has every editable field: send the current values with the changed one.
	private update(changes: {
		clientId?: string;
		displayName?: string;
		tenantId?: string | null;
		initiateLoginUri?: string | null;
	}) {
		return updateClient({
			id: this.id,
			clientId: changes.clientId ?? this._clientId,
			displayName: changes.displayName ?? this._displayName,
			tenantId: changes.tenantId !== undefined ? changes.tenantId : this._tenantId,
			initiateLoginUri:
				changes.initiateLoginUri !== undefined ? changes.initiateLoginUri : this._initiateLoginUri
		});
	}

	changeDisplayName = async (displayName: string) => {
		await this.update({ displayName });
		this._displayName = displayName;
	};

	changeClientId = async (clientId: string) => {
		await this.update({ clientId });
		this._clientId = clientId;
	};

	// A blank value clears the address.
	changeInitiateLoginUri = async (uri: string) => {
		const initiateLoginUri = uri.trim() === '' ? null : uri.trim();
		await this.update({ initiateLoginUri });
		this._initiateLoginUri = initiateLoginUri;
	};

	changeTenantId = async (tenantId: string | null) => {
		await this.update({ tenantId });
		const newTenant = tenantId ? await getTenantById(tenantId) : null;
		this._tenantId = tenantId;
		this._tenantName = newTenant?.name ?? '';
	};

	addRedirectUri = async (uri: string) => {
		await addClientRedirectUri({ id: this.id, uri });
		this._redirectUris = [...this._redirectUris, uri];
	};

	removeRedirectUri = async (uri: string) => {
		await removeClientRedirectUri({ id: this.id, uri });
		this._redirectUris = this._redirectUris.filter((u) => u !== uri);
	};

	addPostLogoutRedirectUri = async (uri: string) => {
		await addClientPostLogoutRedirectUri({ id: this.id, uri });
		this._postLogoutRedirectUris = [...this._postLogoutRedirectUris, uri];
	};

	removePostLogoutRedirectUri = async (uri: string) => {
		await removeClientPostLogoutRedirectUri({ id: this.id, uri });
		this._postLogoutRedirectUris = this._postLogoutRedirectUris.filter((u) => u !== uri);
	};

	addScope = async (scope: ScopeDto) => {
		await addClientScope({ id: this.id, scopeId: scope.id });
		this._scopes = [...this._scopes, scope];
	};

	removeScope = async (scopeId: string) => {
		await removeClientScope({ id: this.id, scopeId });
		this._scopes = this._scopes.filter((s) => s.id !== scopeId);
	};
}
