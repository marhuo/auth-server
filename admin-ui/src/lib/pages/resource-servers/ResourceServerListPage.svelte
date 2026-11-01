<script lang="ts">
	import type { ResourceServerDto } from '#lib/api/generated/index.js';
	import { getResourceServerPagedList } from '#lib/api/resourceServerApi.js';
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import { PagedList } from '#lib/models/PagedList.svelte.js';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';

	const list = new PagedList<ResourceServerDto>(getResourceServerPagedList);
	list.load();
</script>

<svelte:head>
	<title>Resurssipalvelimet</title>
</svelte:head>
<div class="card border border-base-300">
	<div class="card-body">
		<h1 class="mb-4 card-title">Resurssipalvelimet</h1>

		<div class="flex items-center justify-between gap-4">
			<label class="input w-full max-w-sm">
				<Icon icon="ic:outline-search" width="18" class="opacity-50" />
				<input
					type="search"
					placeholder="Hae"
					value={list.searchTerm}
					oninput={(e) => list.setSearchTerm(e.currentTarget.value)}
				/>
			</label>

			<a href={resolve('/resource-servers/create')} class="btn btn-ghost btn-sm">
				<Icon icon="ic:baseline-add" width="18" />
				Lisää uusi
			</a>
		</div>

		<table class="table">
			<thead>
				<tr>
					<th>Nimi</th>
					<th>Tunniste (audience)</th>
				</tr>
			</thead>
			<tbody>
				{#each list.items as rs (rs.id)}
					<tr class="hover:bg-base-200">
						<td>
							<a href={resolve('/resource-servers/[id]', { id: rs.id })} class="link">
								{rs.displayName}
							</a>
						</td>
						<td>{rs.audience}</td>
					</tr>
				{/each}
			</tbody>
		</table>

		<CompactPagination {list} />
	</div>
</div>
