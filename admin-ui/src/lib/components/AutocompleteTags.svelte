<script lang="ts">
	import AutocompleteCombobox, {
		type SelectOption
	} from '#lib/components/AutocompleteCombobox.svelte';

	interface Props {
		value: SelectOption[];
		search: (text: string) => Promise<SelectOption[]>;
		errors?: string[];
		hint?: string;
		disabled?: boolean;
		class?: string;
	}

	let {
		value = $bindable([]),
		search,
		errors = [],
		hint,
		class: className,
		disabled = false
	}: Props = $props();

	function add(option: SelectOption | null) {
		if (!option) return;
		if (value.some((v) => v.value === option.value)) return;
		value = [...value, option];
	}

	function remove(optionValue: string) {
		value = value.filter((v) => v.value !== optionValue);
	}

	async function searchUnselected(text: string): Promise<SelectOption[]> {
		const results = await search(text);
		return results.filter((r) => !value.some((v) => v.value === r.value));
	}
</script>

<div class={className}>
	{#if value.length > 0}
		<div class="mb-2 flex flex-wrap gap-2">
			{#each value as option (option.value)}
				<span class="badge gap-1 badge-soft">
					{option.label}
					{#if !disabled}
						<button
							type="button"
							class="cursor-pointer opacity-60 hover:opacity-100"
							aria-label="Poista {option.label}"
							onclick={() => remove(option.value)}
						>
							✕
						</button>
					{/if}
				</span>
			{/each}
		</div>
	{/if}

	<AutocompleteCombobox
		bind:value={() => null, add}
		search={searchUnselected}
		{errors}
		{hint}
		{disabled}
	/>
</div>
