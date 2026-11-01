// src/lib/models/clientTypes.ts
import type { ClientDtoTypes } from '#lib/api/generated/clientDtoTypes.ts';

type ClientTypeInfo = {
	label: string;
	description: string;
	needsSecret: boolean;
	hasUsers: boolean;
};

// Record forces a label for every type: adding a new type on the backend breaks the build here.
const clientTypes: Record<ClientDtoTypes, ClientTypeInfo> = {
	PublicApplication: {
		label: 'Selain- tai mobiilisovellus',
		description:
			'Käyttäjä kirjautuu. Sovellus toimii käyttäjän laitteella eikä voi säilyttää salaisuutta.',
		needsSecret: false,
		hasUsers: true
	},
	ConfidentialApplication: {
		label: 'Palvelinsovellus',
		description: 'Käyttäjä kirjautuu. Sovelluksella on oma palvelin, joka säilyttää salaisuuden.',
		needsSecret: true,
		hasUsers: true
	},
	ConfidentialMachine: {
		label: 'Järjestelmäintegraatio',
		description: 'Ohjelma kutsuu rajapintaa itse ilman käyttäjää, esimerkiksi taustaajo.',
		needsSecret: true,
		hasUsers: false
	}
};

export const clientTypeLabel = (type: ClientDtoTypes) => clientTypes[type].label;

export const clientTypeDescription = (type: ClientDtoTypes) => clientTypes[type].description;

export const clientTypeNeedsSecret = (type: ClientDtoTypes) => clientTypes[type].needsSecret;

export const clientTypeHasUsers = (type: ClientDtoTypes) => clientTypes[type].hasUsers;
