export function normalizeRoleName(value: string): string {
	return value
		.toLowerCase()
		.replace(/[^a-z0-9-_]/g, '')
		.replace(/-+/g, '-')
		.replace(/^-/, '');
}
export function normalizeScopeName(value: string): string {
	return value
		.toLowerCase()
		.replace(/[^a-z0-9\-:.]/g, '')
		.replace(/[-:.]{2,}/g, (match) => match.charAt(0))
		.replace(/^[-:.]/, '');
}

export function normalizeSlug(value: string): string {
	return value
		.toLowerCase()
		.replace(/[^a-z0-9\-:.]/g, '')
		.replace(/[-:.]{2,}/g, (match) => match.charAt(0))
		.replace(/^[-:.]/, '');
}
export function normalizeAudience(value: string): string {
	return value
		.toLowerCase()
		.replace(/[^a-z0-9\-:.]/g, '')
		.replace(/[-:.]{2,}/g, (match) => match.charAt(0))
		.replace(/^[-:.]/, '');
}
export function normalizeClientId(value: string): string {
	return value
		.replace(/[^a-zA-Z0-9._-]/g, '') // vain sallitut merkit
		.replace(/^[._-]+/, '') // ensimmäinen merkki kirjain tai numero
		.slice(0, 100); // enintään 100 merkkiä
}
