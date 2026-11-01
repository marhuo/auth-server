<script lang="ts">
	import { DateInput } from 'date-picker-svelte';
	import { getFieldId } from '#lib/components/FormField.svelte';

	interface Props {
		value: string | null;
		min?: Date;
		max?: Date;
		errors?: string[];
		hint?: string;
		required?: boolean;
		disabled?: boolean;
	}

	const thisYear = new Date().getFullYear();

	let {
		value = $bindable(null),
		min = new Date(2026, 0, 1),
		max = new Date(thisYear + 10, 11, 31),
		errors = [],
		hint,
		required = false,
		disabled = false
	}: Props = $props();

	const ownId = $props.id();
	const id = getFieldId() ?? ownId;

	const fiLocale = {
		weekdays: ['su', 'ma', 'ti', 'ke', 'to', 'pe', 'la'],
		months: [
			'tammikuu',
			'helmikuu',
			'maaliskuu',
			'huhtikuu',
			'toukokuu',
			'kesäkuu',
			'heinäkuu',
			'elokuu',
			'syyskuu',
			'lokakuu',
			'marraskuu',
			'joulukuu'
		],
		weekStartsOn: 1
	};

	const date = $derived(value ? new Date(value) : null);

	function setDate(d: Date | null) {
		value = d ? d.toISOString() : null;
	}
</script>

<div
	class="dtf relative"
	style="
		--date-input-width: 100%;
		--date-picker-background: var(--color-base-100);
		--date-picker-foreground: var(--color-base-content);
		--date-picker-highlight-border: var(--color-primary);
		--date-picker-selected-background: var(--color-primary);
		--date-picker-selected-color: var(--color-primary-content);
	"
>
	<DateInput
		{id}
		bind:value={() => date, setDate}
		{min}
		{max}
		format="dd.MM.yyyy HH:mm"
		placeholder="pp.kk.vvvv hh:mm"
		timePrecision="minute"
		locale={fiLocale}
		{required}
		{disabled}
	/>

	{#if value && !disabled}
		<button
			type="button"
			class="btn absolute top-1/2 right-2 btn-circle -translate-y-1/2 btn-ghost btn-xs"
			aria-label="Tyhjennä"
			onclick={() => (value = null)}
		>
			✕
		</button>
	{/if}
</div>

{#if hint}
	<p class="label">{hint}</p>
{/if}
{#each errors as msg, index (index)}
	<p class="label text-error">{msg}</p>
{/each}

<style>
	.dtf.dtf :global(input) {
		width: 100%;
		height: 2.5rem;
		padding-inline: 0.75rem;
		padding-right: 2.25rem;
		font-size: 0.875rem;
		color: var(--color-base-content);
		background-color: var(--color-base-100);
		border: 1px solid color-mix(in oklab, var(--color-base-content) 20%, transparent);
		border-radius: var(--radius-field);
	}

	.dtf.dtf :global(input:focus) {
		outline: 2px solid color-mix(in oklab, var(--color-base-content) 20%, transparent);
		outline-offset: 2px;
	}

	.dtf :global(.date-time-picker) {
		zoom: 1.2;
	}
</style>
