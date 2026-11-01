<script lang="ts">
	import type { TenantDto } from '#lib/api/generated/index.js';
	import { getTenantPagedList } from '#lib/api/tenantApi.js';
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import { PagedList } from '#lib/models/PagedList.svelte.js';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';

	const list = new PagedList<TenantDto>(getTenantPagedList);
	list.load();
</script>

<svelte:head>
	<title>Tenantit</title>
</svelte:head>
<div class="card border border-base-300">
	<div class="card-body">
		<div class="mb-4 card-title">
			<h1>Tenantit</h1>
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

			<a href={resolve('/tenants/create')} class="btn btn-ghost btn-sm">
				<Icon icon="ic:baseline-add" width="18" />
				Lisää uusi
			</a>
		</div>

		<table class="table">
			<thead>
				<tr>
					<th>Nimi</th>
				</tr>
			</thead>
			<tbody>
				{#each list.items as tenant (tenant.id)}
					<tr class="hover:bg-base-200">
						<td>
							<a href={resolve('/tenants/[id]', { id: tenant.id })} class="link">{tenant.name}</a>
						</td>
					</tr>
				{/each}
			</tbody>
		</table>

		<CompactPagination {list} />
	</div>
</div>
