<script lang="ts">
	import Icon from '@iconify/svelte';
	import { getFieldId } from '#lib/components/FormField.svelte';

	interface Props {
		value: string;
		errors?: string[];
		hint?: string;
		required?: boolean;
		disabled?: boolean;
	}

	let {
		value = $bindable(),
		errors = [],
		hint,
		required = false,
		disabled = false
	}: Props = $props();

	const ownId = $props.id();
	const id = getFieldId() ?? ownId;

	let visible = $state(false);
</script>

<label class={['validator input w-full', { 'input-error': errors.length }]}>
	<input
		{id}
		type={visible ? 'text' : 'password'}
		autocomplete="new-password"
		{required}
		{disabled}
		bind:value
	/>
	<button
		type="button"
		class="btn btn-square btn-ghost btn-xs"
		aria-label={visible ? 'Piilota salasana' : 'Näytä salasana'}
		onclick={() => (visible = !visible)}
	>
		<Icon icon={visible ? 'ic:outline-visibility-off' : 'ic:outline-visibility'} width="18" />
	</button>
</label>
{#if required}
	<p class="validator-hint hidden">Pakollinen kenttä</p>
{/if}
{#if hint}
	<p class="label">{hint}</p>
{/if}
{#each errors as msg, index (index)}
	<p class="label text-error">{msg}</p>
{/each}
