<script lang="ts">
	import { getFieldId } from '#lib/components/FormField.svelte';

	interface Props {
		value: string;
		type?: string;
		placeholder?: string;
		errors?: string[];
		hint?: string;
		required?: boolean;
		disabled?: boolean;
	}

	let {
		value = $bindable(),
		type = 'text',
		placeholder = '',
		errors = [],
		hint,
		required = false,
		disabled = false
	}: Props = $props();

	const ownId = $props.id();
	const id = getFieldId() ?? ownId;
</script>

<input
	{id}
	{type}
	{placeholder}
	autocomplete="off"
	{required}
	{disabled}
	bind:value
	class={['validator input w-full', { 'input-error': errors.length }]}
/>
{#if required}
	<p class="validator-hint hidden">Pakollinen kenttä</p>
{/if}
{#if hint}
	<p class="label">{hint}</p>
{/if}
{#each errors as msg, index (index)}
	<p class="label text-error">{msg}</p>
{/each}
