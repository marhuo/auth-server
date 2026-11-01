import { createUser } from '#lib/api/userApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';

export class UserCreateModel extends FormModelBase {
	userName = $state('');
	email = $state('');
	password = $state('');

	submit = () =>
		this.run(
			{
				userName: this.userName,
				email: this.email,
				password: this.password
			},
			createUser
		);
}
