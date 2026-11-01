import { createTenant } from '#lib/api/tenantApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';

export class TenantCreateModel extends FormModelBase {
	name = $state('');

	submit = () =>
		this.run(
			{
				name: this.name
			},
			createTenant
		);
}
