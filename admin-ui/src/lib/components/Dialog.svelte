<script lang="ts">
	import { type Snippet } from 'svelte';

	interface Props {
		open: boolean;
		children?: Snippet;
	}
	let { open, children }: Props = $props();
	let htmlDialog: HTMLDialogElement;
	$effect(() => {
		if (open) {
			htmlDialog.showModal();
		} else {
			htmlDialog.close();
		}
	});
</script>

<dialog bind:this={htmlDialog} class="modal" oncancel={(e) => e.preventDefault()}>
	{#if open}
		{@render children?.()}
	{/if}
</dialog>
