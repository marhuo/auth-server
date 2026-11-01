<script lang="ts">
	import Icon from '@iconify/svelte';

	export interface MenuItem {
		title: string;
		icon?: string;
		onClick: () => void;
	}
	interface Props {
		items: MenuItem[];
	}

	const { items }: Props = $props();

	function select(item: MenuItem) {
		item.onClick();
		(document.activeElement as HTMLElement | null)?.blur(); // sulkee valikon
	}
</script>

{#if items.length}
	<div class="dropdown dropdown-end">
		<button class="btn btn-square btn-ghost btn-sm" aria-label="Lisää toimintoja">
			<Icon icon="ic:baseline-more-vert" width="20" />
		</button>
		<ul
			tabindex="-1"
			class="sh menu dropdown-content z-10 w-48 border border-base-300 bg-base-200 p-0 shadow-md"
		>
			{#each items as item (item.title)}
				<li class="m-0">
					<button onclick={() => select(item)} class="rounded-none py-3">
						{#if item.icon}<Icon icon={item.icon} width="18" />{/if}
						{item.title}
					</button>
				</li>
			{/each}
		</ul>
	</div>
{/if}
