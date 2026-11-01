<script lang="ts">
	import { ValidationError } from '#lib/api/httpClient.ts';
	import Dialog from '#lib/components/Dialog.svelte';
	import TextAreaInput from '#lib/components/TextAreaInput.svelte';
	import Icon from '@iconify/svelte';

	interface Props {
		label: string;
		value: string;
		emptyText?: string;
		rows?: number;
		placeholder?: string;
		hint?: string;
		dialogTitle?: string;
		required?: boolean;
		editable?: boolean;
		onSave?: (value: string) => Promise<void>;
	}

	let {
		label,
		value,
		emptyText = 'Ei kuvausta',
		rows = 4,
		placeholder = '',
		hint,
		dialogTitle = label,
		required = false,
		editable = true,
		onSave
	}: Props = $props();

	let editMode = $state(false);
	let inputValue = $state('');
	let errors = $state<string[]>([]);
	let submitting = $state(false);

	const canSave = $derived(
		!submitting && inputValue !== value && (!required || inputValue.trim() !== '')
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
			<div class="whitespace-pre-line">{value}</div>
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
			<form class="modal-box flex flex-col gap-4" onsubmit={onSubmit}>
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
					<TextAreaInput bind:value={inputValue} {rows} {placeholder} {hint} {errors} {required} />
				</fieldset>

				<div class="modal-action">
					<button type="submit" class="btn btn-primary" disabled={!canSave}>
						{#if submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
						Tallenna
					</button>
				</div>
			</form>
		</Dialog>
	{/if}
</li>
