import { inviteUser } from '#lib/api/userApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';

export class UserInviteModel extends FormModelBase {
	email = $state('');

	// Returns the id of the invited user. Roles are added on the user details page.
	// If the email belongs to an existing user with a password, no invitation is sent.
	// No client: an admin invitation is not for a specific app (no sign-in button after the password).
	submit = () => this.run({ email: this.email, userRoles: [], clientId: null }, inviteUser);
}
