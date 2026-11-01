<script lang="ts">
	import Icon from '@iconify/svelte';
	import { getFieldId } from '#lib/components/FormField.svelte';

	interface Props {
		value: string[];
		placeholder?: string;
		addLabel?: string;
		errors?: string[];
		hint?: string;
		disabled?: boolean;
	}

	let {
		value = $bindable([]),
		placeholder = '',
		addLabel = 'Lisää',
		errors = [],
		hint,
		disabled = false
	}: Props = $props();

	const ownId = $props.id();
	const id = getFieldId() ?? ownId;

	function add() {
		value = [...value, ''];
	}

	function remove(index: number) {
		value = value.filter((_, i) => i !== index);
	}

	function update(index: number, text: string) {
		value = value.map((item, i) => (i === index ? text : item));
	}
</script>

<div class="flex flex-col gap-2">
	{#each value as item, index (index)}
		<div class="flex items-center gap-2">
			<input
				id={index === 0 ? id : undefined}
				class="input w-full"
				{placeholder}
				{disabled}
				value={item}
				oninput={(e) => update(index, e.currentTarget.value)}
				autocomplete="off"
			/>
			<button
				type="button"
				class="btn btn-square btn-ghost btn-sm"
				aria-label="Poista"
				{disabled}
				onclick={() => remove(index)}
			>
				<Icon icon="ic:outline-delete" width="18" />
			</button>
		</div>
	{/each}

	<button type="button" class="btn self-start btn-ghost btn-sm" {disabled} onclick={add}>
		<Icon icon="ic:baseline-add" width="18" />
		{addLabel}
	</button>
</div>

{#if hint}
	<p class="label">{hint}</p>
{/if}
{#each errors as msg, index (index)}
	<p class="label text-error">{msg}</p>
{/each}
