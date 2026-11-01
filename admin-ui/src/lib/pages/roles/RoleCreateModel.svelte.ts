import { createRole } from '#lib/api/roleApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';
import { normalizeRoleName } from '#lib/utils/normalizers.ts';

export class RoleCreateModel extends FormModelBase {
	private _name = $state('');
	get name() {
		return this._name;
	}
	set name(value: string) {
		this._name = normalizeRoleName(value);
	}

	description = $state('');
	resourceServerId = $state('');

	constructor(resourceServerId = '') {
		super();
		this.resourceServerId = resourceServerId;
	}

	submit = () =>
		this.run(
			{
				name: this._name,
				description: this.description,
				resourceServerId: this.resourceServerId
			},
			createRole
		);
}
