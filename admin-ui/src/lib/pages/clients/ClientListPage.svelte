<script lang="ts">
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import { ClientListModel } from '#lib/pages/clients/ClientListModel.svelte.ts';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';

	const list = new ClientListModel();
	list.load();
</script>

<svelte:head>
	<title>Clientit</title>
</svelte:head>

<div class="card border border-base-300">
	<div class="card-body">
		<div class="mb-4 card-title">
			<h1>Clientit</h1>
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

			<a href={resolve('/clients/create')} class="btn btn-ghost btn-sm">
				<Icon icon="ic:baseline-add" width="18" />
				Lisää uusi
			</a>
		</div>

		<table class="table">
			<thead>
				<tr>
					<th>Nimi</th>
					<th>Tyyppi</th>
					<th>Resurssipalvelin</th>
					<th>Tenant</th>
				</tr>
			</thead>
			<tbody>
				{#each list.items as client (client.id)}
					<tr class="hover:bg-base-200">
						<td>
							<a href={resolve('/clients/[id]', { id: client.id })} class="link"
								>{client.displayName}</a
							>
						</td>
						<td>{client.typeLabel}</td>
						<td>
							<a
								href={resolve('/resource-servers/[id]', { id: client.resourceServerId })}
								class="link"
							>
								{client.resourceServerName}
							</a>
						</td>
						<td>
							{#if client.tenantId}
								<a href={resolve('/tenants/[id]', { id: client.tenantId })} class="link"
									>{client.tenantName}</a
								>
							{/if}
						</td>
					</tr>
				{/each}
			</tbody>
		</table>

		<CompactPagination {list} />
	</div>
</div>
