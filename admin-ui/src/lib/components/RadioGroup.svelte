<script lang="ts" module>
	export interface RadioOption {
		value: string;
		label: string;
		description?: string;
	}
</script>

<script lang="ts">
	import { getFieldId } from '#lib/components/FormField.svelte';

	interface Props {
		value: string;
		options: RadioOption[];
		errors?: string[];
		required?: boolean;
		disabled?: boolean;
	}

	let {
		value = $bindable(),
		options,
		errors = [],
		required = false,
		disabled = false
	}: Props = $props();

	const ownId = $props.id();
	const id = getFieldId() ?? ownId;
</script>

<div
	class="flex flex-col gap-2"
	role="radiogroup"
	aria-labelledby="{id}-label"
	aria-required={required}
>
	{#each options as option (option.value)}
		<label class="flex cursor-pointer items-start gap-3">
			<input
				type="radio"
				class="radio mt-0.5 radio-sm"
				name={id}
				value={option.value}
				bind:group={value}
				{required}
				{disabled}
			/>
			<div>
				<div>{option.label}</div>
				{#if option.description}
					<div class="text-sm opacity-60">{option.description}</div>
				{/if}
			</div>
		</label>
	{/each}
</div>
{#each errors as msg, index (index)}
	<p class="label text-error">{msg}</p>
{/each}
