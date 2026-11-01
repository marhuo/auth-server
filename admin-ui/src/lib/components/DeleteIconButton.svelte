<script lang="ts">
	import { ValidationError } from '#lib/api/httpClient.ts';
	import Dialog from '#lib/components/Dialog.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import Icon from '@iconify/svelte';
	import type { Snippet } from 'svelte';

	interface Props {
		onDelete: () => Promise<void>;
		onDeleted?: () => void;
		dialogTitle?: string;
		children?: Snippet;
	}
	let { onDelete, onDeleted, dialogTitle = 'Poista', children }: Props = $props();

	let dialogOpen = $state(false);
	let submitting = $state(false);
	let errors: string[] = $state([]);

	async function onDeleteClicked() {
		submitting = true;
		errors = [];
		try {
			await onDelete();
			onDeleted?.();
			dialogOpen = false;
			errors = [];
		} catch (e) {
			if (e instanceof ValidationError) {
				errors = e.errors.map((error) => error.messageFi);
				return;
			}
			throw e;
		} finally {
			submitting = false;
		}
	}
</script>

<button class="btn btn-square btn-ghost" aria-label="Poista" onclick={() => (dialogOpen = true)}>
	<Icon icon="ic:outline-delete" width="20" />
</button>
{#if dialogOpen}
	<Dialog open={true}>
		<div class="modal-box max-w-md">
			<div class="flex items-center justify-between">
				<h3 class="text-lg font-semibold">{dialogTitle}</h3>
				<button
					type="button"
					class="btn btn-square btn-ghost btn-sm"
					aria-label="Sulje"
					disabled={submitting}
					onclick={() => (dialogOpen = false)}
				>
					<Icon icon="ic:baseline-close" width="20" />
				</button>
			</div>

			<GeneralErrors messages={errors} />

			<p class="py-4">
				{#if children}
					{@render children()}
				{:else}
					Oletko varma, että haluat poistaa?
				{/if}
			</p>

			<div class="modal-action">
				<button class="btn btn-error" disabled={submitting} onclick={() => onDeleteClicked()}>
					{#if submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
					Poista
				</button>
			</div>
		</div>
	</Dialog>
{/if}
