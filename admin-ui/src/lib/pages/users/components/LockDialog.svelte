<script lang="ts">
	import { ValidationError } from '#lib/api/httpClient.ts';
	import Dialog from '#lib/components/Dialog.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import Icon from '@iconify/svelte';

	interface Props {
		isLocked: boolean;
		email: string;
		onLockToggle: (isLocked: boolean) => Promise<void>;
		onCancel: () => void;
	}
	let { isLocked, email, onLockToggle, onCancel }: Props = $props();
	let submitting = $state(false);
	let errors = $state<string[]>([]);

	async function onSubmitClick() {
		submitting = true;
		errors = [];
		try {
			await onLockToggle(!isLocked);
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
	<div class="modal-box flex min-h-44 flex-col gap-6 overflow-visible">
		<div class="flex items-center justify-between">
			<h3 class="text-lg font-semibold">
				{isLocked ? 'Poista lukitus' : 'Lukitse käyttäjä'}
			</h3>
			<button
				type="button"
				class="btn btn-square btn-ghost btn-sm"
				aria-label="Sulje"
				disabled={submitting}
				onclick={onCancel}
			>
				<Icon icon="ic:baseline-close" width="20" />
			</button>
		</div>

		<GeneralErrors messages={errors} />

		{#if isLocked}
			<div>
				Poista lukitus käyttäjältä <strong>{email}</strong>
			</div>
			<button
				type="submit"
				class="btn self-end btn-primary"
				disabled={submitting}
				onclick={onSubmitClick}
			>
				{#if submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
				Poista lukitus
			</button>
		{:else}
			<div>
				Lukitse käyttäjä <strong>{email}</strong>
			</div>
			<button
				type="submit"
				class="btn self-end btn-outline btn-error"
				disabled={submitting}
				onclick={onSubmitClick}
			>
				{#if submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
				Lukitse käyttäjä
			</button>
		{/if}
	</div>
</Dialog>
