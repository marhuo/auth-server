<script lang="ts">
	import { ValidationError } from '#lib/api/httpClient.ts';
	import Dialog from '#lib/components/Dialog.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import Icon from '@iconify/svelte';

	interface Props {
		label: string;
		value: string;
		type?: 'text' | 'email' | 'url' | 'number' | 'tel' | 'search';
		placeholder?: string;
		hint?: string;
		dialogTitle?: string;
		required?: boolean;
		mono?: boolean;
		editable?: boolean;
		normalize?: (value: string) => string;
		onSave?: (value: string) => Promise<void>;
	}

	let {
		label,
		value,
		type = 'text',
		placeholder = '',
		hint,
		dialogTitle = `${label}`,
		required = false,
		mono = false,
		editable = true,
		normalize,
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

	function setInputValue(v: string) {
		inputValue = normalize ? normalize(v) : v;
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
		<div class={{ 'font-mono': mono }}>{value}</div>
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
					<TextInput
						{type}
						{placeholder}
						{hint}
						{errors}
						{required}
						bind:value={() => inputValue, setInputValue}
					/>
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
