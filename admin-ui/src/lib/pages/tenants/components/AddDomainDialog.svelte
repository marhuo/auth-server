<script lang="ts">
	import { ValidationError } from '#lib/api/httpClient.ts';
	import Dialog from '#lib/components/Dialog.svelte';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import Icon from '@iconify/svelte';

	interface Props {
		onAdd: (name: string) => Promise<void>;
		onClose: () => void;
	}

	let { onAdd, onClose }: Props = $props();

	let name = $state('');
	let submitting = $state(false);
	let errors = $state<string[]>([]);

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		submitting = true;
		errors = [];
		try {
			await onAdd(name.trim());
			onClose();
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

<Dialog open={true}>
	<form class="modal-box flex flex-col gap-4" onsubmit={onSubmit}>
		<div class="flex items-center justify-between">
			<h3 class="text-lg font-semibold">Lisää domain</h3>
			<button
				type="button"
				class="btn btn-square btn-ghost btn-sm"
				aria-label="Sulje"
				disabled={submitting}
				onclick={onClose}
			>
				<Icon icon="ic:baseline-close" width="20" />
			</button>
		</div>

		<fieldset class="fieldset" disabled={submitting}>
			<FormField>
				<FormLabel>Domain</FormLabel>
				<TextInput
					bind:value={name}
					placeholder="acme.fi"
					hint="Tenantti vahvistaa domainin lisäämällä sen DNS:ään TXT-rivin."
					{errors}
					required
				/>
			</FormField>
		</fieldset>

		<div class="modal-action">
			<button type="submit" class="btn btn-primary" disabled={submitting || !name.trim()}>
				{#if submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
				Lisää
			</button>
		</div>
	</form>
</Dialog>
