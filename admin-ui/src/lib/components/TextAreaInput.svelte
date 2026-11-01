<script lang="ts">
	import { getFieldId } from '#lib/components/FormField.svelte';

	interface Props {
		value: string;
		rows?: number;
		placeholder?: string;
		errors?: string[];
		hint?: string;
		required?: boolean;
		disabled?: boolean;
	}

	let {
		value = $bindable(),
		rows = 3,
		placeholder = '',
		errors = [],
		hint,
		required = false,
		disabled = false
	}: Props = $props();

	const ownId = $props.id();
	const id = getFieldId() ?? ownId;
</script>

<textarea
	{id}
	{rows}
	{placeholder}
	{required}
	{disabled}
	bind:value
	class={['validator textarea w-full', { 'textarea-error': errors.length }]}></textarea>
{#if required}
	<p class="validator-hint hidden">Pakollinen kenttä</p>
{/if}
{#if hint}
	<p class="label">{hint}</p>
{/if}
{#each errors as msg, index (index)}
	<p class="label text-error">{msg}</p>
{/each}
