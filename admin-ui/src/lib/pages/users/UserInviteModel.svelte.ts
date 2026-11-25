import { inviteUser } from '#lib/api/userApi.ts';
import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';

export class UserInviteModel extends FormModelBase {
	email = $state('');

	// Returns the id of the invited user. Roles are added on the user details page.
	// If the email belongs to a user who has already signed in, no invitation is sent.
	// No client: an admin invitation is not for a specific app, so the email links to the auth server.
	submit = () =>
		this.run({ email: this.email, userRoles: [], clientId: null, language: null }, inviteUser);
}
