import { createResourceServer } from '#lib/api/resourceServerApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';

export class ResourceServerCreateModel extends FormModelBase {
	displayName = $state('');
	audience = $state('');

	submit = () =>
		this.run(
			{
				displayName: this.displayName,
				audience: this.audience
			},
			createResourceServer
		);
}
