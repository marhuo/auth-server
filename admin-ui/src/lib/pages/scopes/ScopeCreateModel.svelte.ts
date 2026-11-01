import { createScope } from '#lib/api/scopeApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';
import { normalizeScopeName } from '#lib/utils/normalizers.ts';

export class ScopeCreateModel extends FormModelBase {
	private _name = $state('');
	get name() {
		return this._name;
	}
	set name(value: string) {
		this._name = normalizeScopeName(value);
	}

	displayName = $state('');
	resourceServerId = $state('');

	constructor(resourceServerId = '') {
		super();
		this.resourceServerId = resourceServerId;
	}

	submit = () =>
		this.run(
			{
				name: this._name,
				displayName: this.displayName,
				resourceServerId: this.resourceServerId || null
			},
			createScope
		);
}
