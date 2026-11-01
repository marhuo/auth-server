<script lang="ts">
	import {
		type ResourceServerDto,
		type ScopeDto,
		type ScopePagedListRequest
	} from '#lib/api/generated/index.js';
	import { getResourceServersByIds } from '#lib/api/resourceServerApi.js';
	import { getScopePagedList } from '#lib/api/scopeApi.js';
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import { PagedList } from '#lib/models/PagedList.svelte.js';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';

	let resourceServers = $state(new Map<string, ResourceServerDto>());
	const list = new PagedList<ScopeDto, ScopePagedListRequest>(
		async (request) => {
			const page = await getScopePagedList(request);
			const ids = [
				...new Set(page.items.map((s) => s.resourceServerId).filter((id): id is string => !!id))
			];
			const servers = ids.length ? await getResourceServersByIds(ids) : [];
			resourceServers = new Map(servers.map((rs) => [rs.id, rs]));
			return page;
		},
		{ resourceServerId: null }
	);
	list.load();
</script>

<svelte:head>
	<title>Scopet</title>
</svelte:head>
<div class="card border border-base-300">
	<div class="card-body">
		<div class="mb-4 card-title">
			<h1>Scopet</h1>
		</div>
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

			<a href={resolve('/scopes/create')} class="btn btn-ghost btn-sm">
				<Icon icon="ic:baseline-add" width="18" />
				Lisää uusi
			</a>
		</div>

		<table class="table">
			<thead>
				<tr>
					<th>Nimi</th>
					<th>Näytettävä nimi</th>
					<th>Resurssipalvelin</th>
				</tr>
			</thead>
			<tbody>
				{#each list.items as scope (scope.id)}
					<tr class="hover:bg-base-200">
						<td>
							<a href={resolve('/scopes/[id]', { id: scope.id })} class="link">{scope.name}</a>
						</td>
						<td>{scope.displayName}</td>
						<td>
							{#if scope.resourceServerId}
								<a
									href={resolve('/resource-servers/[id]', { id: scope.resourceServerId })}
									class="link"
								>
									{resourceServers.get(scope.resourceServerId)?.displayName}
								</a>
							{/if}
						</td>
					</tr>
				{/each}
			</tbody>
		</table>

		<CompactPagination {list} />
	</div>
</div>
