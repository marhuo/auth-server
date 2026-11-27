<script lang="ts">
	import { type Snippet } from 'svelte';

	interface Props {
		open: boolean;
		// Called on Esc and on a click outside the dialog. Without it the dialog closes only from its own buttons.
		onClose?: () => void;
		children?: Snippet;
	}
	let { open, onClose, children }: Props = $props();
	let htmlDialog: HTMLDialogElement;
	$effect(() => {
		if (open) {
			htmlDialog.showModal();
		} else {
			htmlDialog.close();
		}
	});

	// The browser never closes the dialog itself: the page owns the open state.
	function onCancel(e: Event) {
		e.preventDefault();
		onClose?.();
	}
</script>

<dialog bind:this={htmlDialog} class="modal" oncancel={onCancel}>
	{#if open}
		{@render children?.()}
	{/if}
	{#if onClose}
		<button type="button" class="modal-backdrop" tabindex="-1" aria-label="Sulje" onclick={onClose}
		></button>
	{/if}
</dialog>
