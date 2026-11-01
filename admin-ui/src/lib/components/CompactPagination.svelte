<script lang="ts">
	import type { Pageable } from '#lib/models/PagedList.svelte.ts';

	interface Props {
		list: Pageable;
	}
	const { list }: Props = $props();
</script>

{#if list.totalPages > 1}
	<div class="flex flex-col items-center gap-2 self-center">
		<div class="join">
			<button
				class="btn join-item btn-sm"
				disabled={!list.hasPrevious}
				onclick={() => list.setPageNumber(list.pageNumber - 1)}
			>
				Edellinen
			</button>
			<button
				class="btn join-item btn-sm"
				disabled={!list.hasNext}
				onclick={() => list.setPageNumber(list.pageNumber + 1)}
			>
				Seuraava
			</button>
		</div>
		<span class="text-sm opacity-70">Sivu {list.pageNumber} / {list.totalPages}</span>
	</div>
{/if}
