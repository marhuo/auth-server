<script lang="ts">
	import { ValidationError } from '#lib/api/httpClient.ts';

	interface Props {
		onVerify: () => Promise<void>;
	}

	let { onVerify }: Props = $props();

	let submitting = $state(false);
	let errors = $state<string[]>([]);

	async function onClick() {
		submitting = true;
		errors = [];
		try {
			await onVerify();
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

<button class="btn btn-sm" disabled={submitting} onclick={onClick}>
	{#if submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
	Tarkista
</button>
{#each errors as error, index (index)}
	<p class="text-sm text-error">{error}</p>
{/each}
