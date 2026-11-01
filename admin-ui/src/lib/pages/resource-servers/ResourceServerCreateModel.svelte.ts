import { createResourceServer } from '#lib/api/resourceServerApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';

export class ResourceServerCreateModel extends FormModelBase {
	displayName = $state('');
	audience = $state('');
	// Secret of the backend client that is created with the resource server.
	secret = $state('');

	// Same as ResourceServer.BackendClientId in the backend.
	get backendClientId() {
		return this.audience.trim() ? `${this.audience.trim()}-backend` : '';
	}

	submit = () =>
		this.run(
			{
				displayName: this.displayName,
				audience: this.audience,
				secret: this.secret
			},
			createResourceServer
		);
}
