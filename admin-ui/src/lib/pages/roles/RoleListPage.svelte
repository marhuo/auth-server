<script lang="ts">
	import {
		type ResourceServerDto,
		type RoleDto,
		type RolePagedListRequest
	} from '#lib/api/generated/index.js';
	import { getResourceServersByIds } from '#lib/api/resourceServerApi.js';
	import { getRolePagedList } from '#lib/api/roleApi.js';
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import { PagedList } from '#lib/models/PagedList.svelte.js';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';

	let resourceServers = $state(new Map<string, ResourceServerDto>());

	const list = new PagedList<RoleDto, RolePagedListRequest>(
		async (request) => {
			const page = await getRolePagedList(request);
			const ids = [...new Set(page.items.map((role) => role.resourceServerId))];
			const servers = ids.length ? await getResourceServersByIds(ids) : [];
			resourceServers = new Map(servers.map((rs) => [rs.id, rs]));
			return page;
		},
		{ resourceServerId: null }
	);
	list.load();
</script>

<svelte:head>
	<title>Roolit</title>
</svelte:head>
<div class="card border border-base-300">
	<div class="card-body">
		<h1 class="mb-4 card-title">Roolit</h1>

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

			<a href={resolve('/roles/create')} class="btn btn-ghost btn-sm">
				<Icon icon="ic:baseline-add" width="18" />
				Lisää uusi
			</a>
		</div>

		<table class="table">
			<thead>
				<tr>
					<th>Nimi</th>
					<th>Kuvaus</th>
					<th>Resurssipalvelin</th>
				</tr>
			</thead>
			<tbody>
				{#each list.items as role (role.id)}
					<tr class="hover:bg-base-200">
						<td><a href={resolve('/roles/[id]', { id: role.id })} class="link">{role.name}</a></td>
						<td>{role.description}</td>
						<td>
							<a
								href={resolve('/resource-servers/[id]', { id: role.resourceServerId })}
								class="link"
							>
								{resourceServers.get(role.resourceServerId)?.displayName}
							</a>
						</td>
					</tr>
				{/each}
			</tbody>
		</table>

		<CompactPagination {list} />
	</div>
</div>
