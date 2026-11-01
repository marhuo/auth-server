<script lang="ts">
	import type { TenantDto } from '#lib/api/generated/index.ts';
	import { getTenantById, getTenantPagedList } from '#lib/api/tenantApi.ts';
	import AutocompleteCombobox, {
		type SelectOption
	} from '#lib/components/AutocompleteCombobox.svelte';

	interface Props {
		value: string | null;
		errors?: string[];
		hint?: string;
		required?: boolean;
		disabled?: boolean;
	}

	let {
		value = $bindable(null),
		errors = [],
		hint,
		required = false,
		disabled = false
	}: Props = $props();

	let tenant = $state<TenantDto | null>(null);
	let searchResults: TenantDto[] = [];

	// Kun arvo muuttuu ulkopuolelta (esim. muokkaussivun data latautuu), haetaan nimi
	$effect(() => {
		const id = value;
		if (!id) {
			tenant = null;
			return;
		}
		if (tenant?.id === id) return;
		getTenantById(id).then((t) => {
			if (value === id) tenant = t;
		});
	});

	function getSelected(): SelectOption | null {
		return tenant ? { value: tenant.id, label: tenant.name } : null;
	}

	function setSelected(option: SelectOption | null) {
		tenant = searchResults.find((t) => t.id === option?.value) ?? null;
		value = option?.value ?? null;
	}

	async function searchTenants(text: string): Promise<SelectOption[]> {
		const result = await getTenantPagedList({
			pageNumber: 1,
			pageSize: 10,
			searchTerm: text || null
		});
		searchResults = result.items;
		return searchResults.map((t) => ({ value: t.id, label: t.name }));
	}
</script>

<AutocompleteCombobox
	bind:value={getSelected, setSelected}
	search={searchTenants}
	{errors}
	{hint}
	{required}
	{disabled}
/>
