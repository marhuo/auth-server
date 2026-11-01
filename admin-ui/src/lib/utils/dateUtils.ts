const dateFormat = new Intl.DateTimeFormat('fi-FI', { dateStyle: 'short' });
const dateTimeFormat = new Intl.DateTimeFormat('fi-FI', { dateStyle: 'short', timeStyle: 'short' });

export function formatIsoDate(iso: string | null | undefined): string {
	return iso ? dateFormat.format(new Date(iso)) : '';
}

export function formatIsoDateTime(iso: string | null | undefined): string {
	return iso ? dateTimeFormat.format(new Date(iso)) : '';
}

// Formats a duration in the largest whole unit, e.g. 3600 → "1 tunti", 2592000 → "30 päivää".
export function formatDurationSeconds(seconds: number): string {
	if (seconds % 86400 === 0) {
		const days = seconds / 86400;
		return `${days} ${days === 1 ? 'päivä' : 'päivää'}`;
	}
	if (seconds % 3600 === 0) {
		const hours = seconds / 3600;
		return `${hours} ${hours === 1 ? 'tunti' : 'tuntia'}`;
	}
	if (seconds % 60 === 0) {
		const minutes = seconds / 60;
		return `${minutes} ${minutes === 1 ? 'minuutti' : 'minuuttia'}`;
	}
	return `${seconds} ${seconds === 1 ? 'sekunti' : 'sekuntia'}`;
}
