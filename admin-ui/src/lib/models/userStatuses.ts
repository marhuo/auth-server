// src/lib/models/userStatuses.ts
import type { UserDtoStatuses } from '#lib/api/generated/userDtoStatuses.ts';

type UserStatusInfo = {
	label: string;
	description: string;
	badgeClass: string;
};

// Record forces a label for every status: adding a new status on the backend breaks the build here.
const userStatuses: Record<UserDtoStatuses, UserStatusInfo> = {
	Invited: {
		label: 'Kutsuttu',
		description: 'Käyttäjä on kutsuttu, mutta ei ole vielä kirjautunut.',
		badgeClass: 'badge-warning'
	},
	Active: {
		label: 'Aktiivinen',
		description: 'Käyttäjä on kirjautunut ainakin kerran.',
		badgeClass: 'badge-success'
	}
};

export const userStatusLabel = (status: UserDtoStatuses) => userStatuses[status].label;

export const userStatusDescription = (status: UserDtoStatuses) => userStatuses[status].description;

export const userStatusBadgeClass = (status: UserDtoStatuses) => userStatuses[status].badgeClass;

export const userStatusIsInvited = (status: UserDtoStatuses) => status === 'Invited';
