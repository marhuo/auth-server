<script lang="ts">
	import type { ScopeDto } from '#lib/api/generated/index.ts';
	import type { SelectOption } from '#lib/components/AutocompleteCombobox.svelte';
	import AutocompleteTags from '#lib/components/AutocompleteTags.svelte';

	interface Props {
		value: ScopeDto[];
		options: ScopeDto[];
		errors?: string[];
		hint?: string;
		disabled?: boolean;
		class?: string;
	}

	let {
		value = $bindable([]),
		options,
		errors = [],
		hint,
		disabled = false,
		class: className
	}: Props = $props();

	function toOption(scope: ScopeDto): SelectOption {
		return { value: scope.id, label: scope.name };
	}

	const selected = $derived(value.map(toOption));

	function setSelected(selectedOptions: SelectOption[]) {
		const all = [...value, ...options];
		value = selectedOptions.map((o) => all.find((s) => s.id === o.value)!);
	}

	async function search(text: string): Promise<SelectOption[]> {
		const term = text.trim().toLowerCase();
		return options.filter((s) => !term || s.name.toLowerCase().includes(term)).map(toOption);
	}
</script>

<AutocompleteTags
	bind:value={() => selected, setSelected}
	{search}
	{errors}
	{hint}
	{disabled}
	class={className}
/>
