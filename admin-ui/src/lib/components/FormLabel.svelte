<script lang="ts">
	import type { Snippet } from 'svelte';
	import { getFieldId } from '#lib/components/FormField.svelte';

	interface Props {
		required?: boolean;
		children: Snippet;
	}

	let { required = false, children }: Props = $props();

	const id = getFieldId();
</script>

<label class="label text-sm" id="{id}-label" for={id}>
	{@render children()}
	{#if required}
		<span class="text-error" aria-hidden="true">*</span>
	{/if}
</label>
