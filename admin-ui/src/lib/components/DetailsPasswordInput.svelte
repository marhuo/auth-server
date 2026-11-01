<script lang="ts">
	import { ValidationError } from '#lib/api/httpClient.ts';
	import Dialog from '#lib/components/Dialog.svelte';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import PasswordInput from '#lib/components/PasswordInput.svelte';
	import Icon from '@iconify/svelte';

	// Shows only whether a password is set; the password itself is never shown.
	interface Props {
		label: string;
		hasPassword: boolean;
		dialogTitle?: string;
		editable?: boolean;
		onSave?: (password: string) => Promise<void>;
	}

	let { label, hasPassword, dialogTitle = label, editable = true, onSave }: Props = $props();

	let editMode = $state(false);
	let password = $state('');
	let confirmPassword = $state('');
	let errors = $state<string[]>([]);
	let submitting = $state(false);

	const mismatch = $derived(confirmPassword !== '' && confirmPassword !== password);
	const canSave = $derived(!submitting && password !== '' && confirmPassword === password);

	function openDialog() {
		password = '';
		confirmPassword = '';
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
			await onSave?.(password);
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
		{#if hasPassword}
			<div>Asetettu</div>
		{:else}
			<div class="opacity-60">Ei asetettu</div>
		{/if}
	</div>

	{#if editable && onSave}
		<button
			type="button"
			class="btn btn-square btn-ghost btn-sm"
			aria-label="Vaihda {label}"
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

				<fieldset class="fieldset gap-4" disabled={submitting}>
					<FormField>
						<FormLabel>Uusi salasana</FormLabel>
						<PasswordInput bind:value={password} {errors} required />
					</FormField>

					<FormField>
						<FormLabel>Vahvista salasana</FormLabel>
						<PasswordInput
							bind:value={confirmPassword}
							errors={mismatch ? ['Salasanat eivät täsmää'] : []}
							required
						/>
					</FormField>
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
