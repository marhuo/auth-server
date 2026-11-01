import {
	deleteResourceServer,
	getResourceServerById,
	updateResourceServer
} from '#lib/api/resourceServerApi.ts';

export class ResourceServerDetailsModel {
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

	private _displayName = $state('');
	get displayName() {
		return this._displayName;
	}

	private _audience = $state('');
	get audience() {
		return this._audience;
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
			const rs = await getResourceServerById(this.id);
			if (!rs) {
				this._notFound = true;
				return;
			}
			this._displayName = rs.displayName;
			this._audience = rs.audience;
			this._isAuthServer = rs.isAuthServer;
			this._createdAt = rs.createdAt;
			this._notFound = false;
		} finally {
			this._loading = false;
		}
	};

	changeDisplayName = async (displayName: string) => {
		await updateResourceServer({ id: this.id, displayName, audience: this._audience });
		this._displayName = displayName;
	};

	changeAudience = async (audience: string) => {
		await updateResourceServer({ id: this.id, displayName: this._displayName, audience });
		this._audience = audience;
	};

	delete = () => deleteResourceServer(this.id);
}
