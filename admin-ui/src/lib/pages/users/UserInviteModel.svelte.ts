import { inviteUser } from '#lib/api/userApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';

export class UserInviteModel extends FormModelBase {
	email = $state('');

	// Returns the id of the invited user. Roles are added on the user details page.
	// If the email belongs to an existing user with a password, no invitation is sent.
	submit = () => this.run({ email: this.email, userRoles: [] }, inviteUser);
}
