<script lang="ts">
	import { ValidationError } from '#lib/api/httpClient.ts';
	import DateTimeInput from '#lib/components/DateTimeInput.svelte';
	import Dialog from '#lib/components/Dialog.svelte';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.ts';
	import Icon from '@iconify/svelte';

	interface Props {
		label: string;
		value: string | null;
		emptyText?: string;
		min?: Date;
		max?: Date;
		hint?: string;
		dialogTitle?: string;
		required?: boolean;
		editable?: boolean;
		onSave?: (value: string | null) => Promise<void>;
	}

	let {
		label,
		value,
		emptyText = 'Ei asetettu',
		min,
		max,
		hint,
		dialogTitle = label,
		required = false,
		editable = true,
		onSave
	}: Props = $props();

	let editMode = $state(false);
	let inputValue = $state<string | null>(null);
	let errors = $state<string[]>([]);
	let submitting = $state(false);

	function sameTime(a: string | null, b: string | null) {
		if (!a || !b) return a === b;
		return new Date(a).getTime() === new Date(b).getTime();
	}

	const canSave = $derived(
		!submitting && !sameTime(inputValue, value) && (!required || inputValue !== null)
	);

	function openDialog() {
		inputValue = value;
		errors = [];
		editMode = true;
	}

	function closeDialog() {
		editMode = false;
	}

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		submitting = true;
		errors = [];
		try {
			await onSave?.(inputValue);
			editMode = false;
		} catch (err) {
			if (err instanceof ValidationError) {
				errors = err.errors.map((x) => x.messageFi);
				return;
			}
			throw err;
		} finally {
			submitting = false;
		}
	}
</script>

<li class="list-row items-center">
	<div class="list-col-grow">
		<div class="text-xs opacity-60">{label}</div>
		{#if value}
			<div>{formatIsoDateTime(value)}</div>
		{:else}
			<div class="opacity-60">{emptyText}</div>
		{/if}
	</div>

	{#if editable && onSave}
		<button
			type="button"
			class="btn btn-square btn-ghost btn-sm"
			aria-label="Muokkaa {label}"
			onclick={openDialog}
		>
			<Icon icon="ic:outline-edit" width="18" />
		</button>
	{/if}

	{#if editMode}
		<Dialog open={true}>
			<form class="modal-box flex min-h-56 flex-col gap-4 overflow-visible" onsubmit={onSubmit}>
				<div class="flex items-center justify-between">
					<h3 class="text-lg font-semibold">{dialogTitle}</h3>
					<button
						type="button"
						class="btn btn-square btn-ghost btn-sm"
						aria-label="Sulje"
						disabled={submitting}
						onclick={closeDialog}
					>
						<Icon icon="ic:baseline-close" width="20" />
					</button>
				</div>

				<fieldset class="fieldset" disabled={submitting}>
					<DateTimeInput bind:value={inputValue} {min} {max} {hint} {errors} {required} />
				</fieldset>

				<div class="modal-action mt-auto">
					<button type="submit" class="btn btn-primary" disabled={!canSave}>
						{#if submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
						Tallenna
					</button>
				</div>
			</form>
		</Dialog>
	{/if}
</li>
