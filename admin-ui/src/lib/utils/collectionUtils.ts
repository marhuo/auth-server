//collectionUtils.ts
export function distinct<T>(arr: T[]): T[] {
	return [...new Set(arr)];
}
export function toMap<TEntry, TMapKey, TMapValue>(
	arr: TEntry[],
	key: (entry: TEntry) => TMapKey,
	value: (entry: TEntry) => TMapValue
): Map<TMapKey, TMapValue> {
	return new Map<TMapKey, TMapValue>(arr.map((entry) => [key(entry), value(entry)]));
}
