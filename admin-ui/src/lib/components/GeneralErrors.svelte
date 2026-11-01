<script lang="ts">
	interface Props {
		messages: string[];
		class?: string;
	}
	const { messages, class: className = '' }: Props = $props();
</script>

{#if messages.length}
	<div role="alert" class={['alert alert-soft alert-error', className]}>
		<div>
			{#each messages as msg, index (index)}
				<div>{msg}</div>
			{/each}
		</div>
	</div>
{/if}
