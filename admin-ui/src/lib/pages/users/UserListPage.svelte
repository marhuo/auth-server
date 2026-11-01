<script lang="ts">
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import { UserList } from '#lib/pages/users/UserList.svelte.ts';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';

	const list = new UserList();
	list.load();
</script>

<svelte:head>
	<title>Käyttäjät</title>
</svelte:head>
<div class="card border border-base-300">
	<div class="card-body">
		<div class="mb-4 card-title">
			<h1>Käyttäjät</h1>
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

			<a href={resolve('/users/create')} class="btn btn-ghost btn-sm">
				<Icon icon="ic:baseline-add" width="18" />
				Lisää uusi
			</a>
		</div>

		<table class="table">
			<thead>
				<tr>
					<th>Käyttäjänimi</th>
					<th>Sähköposti</th>
					<th>Roolit</th>
				</tr>
			</thead>
			<tbody>
				{#each list.items as user (user.id)}
					<tr class="hover:bg-base-200">
						<td>
							<a href={resolve('/users/[id]', { id: user.id })} class="link">{user.userName} </a>
						</td>
						<td>{user.email}</td>
						<td class="flex flex-col gap-4">
							{#each user.roles as role, index (index)}
								<div>
									<a
										href={resolve('/roles/[id]', { id: role.roleId })}
										class="link"
										title={role.roleDescription}
									>
										{role.roleName}
									</a>
									{#if role.tenantId}
										<span>-</span>
										<a href={resolve('/tenants/[id]', { id: role.tenantId })} class="link">
											{role.tenantName}
										</a>
									{/if}
								</div>
							{/each}
						</td>
					</tr>
				{/each}
			</tbody>
		</table>

		<CompactPagination {list} />
	</div>
</div>
