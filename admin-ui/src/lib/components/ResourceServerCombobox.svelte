<script lang="ts">
	import type { ResourceServerDto } from '#lib/api/generated/resourceServerDto.ts';
	import { getResourceServerById, getResourceServerPagedList } from '#lib/api/resourceServerApi.ts';
	import AutocompleteSelect, {
		type SelectOption
	} from '#lib/components/AutocompleteCombobox.svelte';

	interface Props {
		value: string | null;
		errors?: string[];
		hint?: string;
		required?: boolean;
		disabled?: boolean;
		includeAuthServer?: boolean;
	}

	let {
		value = $bindable(null),
		errors = [],
		hint,
		required = false,
		disabled = false,
		includeAuthServer = false
	}: Props = $props();

	let resourceServer = $state<ResourceServerDto | null>(null);
	let searchResults: ResourceServerDto[] = [];

	$effect(() => {
		const id = value;
		if (!id) {
			resourceServer = null;
			return;
		}
		if (resourceServer?.id === id) return;
		getResourceServerById(id).then((rs) => {
			if (value === id) resourceServer = rs;
		});
	});

	function getSelected(): SelectOption | null {
		return resourceServer ? { value: resourceServer.id, label: resourceServer.displayName } : null;
	}

	function setSelected(option: SelectOption | null) {
		resourceServer = searchResults.find((rs) => rs.id === option?.value) ?? null;
		value = option?.value ?? null;
	}

	async function searchServers(text: string): Promise<SelectOption[]> {
		const result = await getResourceServerPagedList({
			pageNumber: 1,
			pageSize: 10,
			searchTerm: text || null
		});
		searchResults = includeAuthServer
			? result.items
			: result.items.filter((rs) => !rs.isAuthServer);
		return searchResults.map((rs) => ({ value: rs.id, label: rs.displayName }));
	}
</script>

<AutocompleteSelect
	bind:value={getSelected, setSelected}
	search={searchServers}
	{errors}
	{hint}
	{required}
	{disabled}
/>
