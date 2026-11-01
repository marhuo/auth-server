<script lang="ts">
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import {
		userStatusBadgeClass,
		userStatusDescription,
		userStatusLabel
	} from '#lib/models/userStatuses.ts';
	import { UserListModel } from '#lib/models/UserListModel.svelte.ts';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.ts';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';

	const list = new UserListModel(null);
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

			<a href={resolve('/users/invite')} class="btn btn-ghost btn-sm">
				<Icon icon="ic:outline-mail" width="18" />
				Kutsu käyttäjä
			</a>
		</div>

		<table class="table">
			<thead>
				<tr>
					<th>Sähköposti</th>
					<th>Roolit</th>
					<th>Tila</th>
					<th>Lukitus</th>
				</tr>
			</thead>
			<tbody>
				{#each list.items as user (user.id)}
					<tr class="hover:bg-base-200">
						<td>
							<a href={resolve('/users/[id]', { id: user.id })} class="link">{user.email}</a>
						</td>

						<!-- Flex inside the cell: a flex td is no longer a table cell and breaks the row layout. -->
						<td>
							<div class="flex flex-col gap-1">
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
								{:else}
									<!-- No roles means no access (users are never deleted, only their roles). -->
									<div>
										<span class="badge badge-soft badge-sm badge-warning">Ei rooleja</span>
									</div>
								{/each}
							</div>
						</td>
						<td>
							<span
								class="badge badge-soft badge-sm {userStatusBadgeClass(user.status)}"
								title={userStatusDescription(user.status)}
							>
								{userStatusLabel(user.status)}
							</span>
						</td>
						<td class="whitespace-nowrap">
							{#if user.locked}
								<span class="badge badge-soft badge-sm badge-error">Lukittu</span>
							{:else if user.lockedOut}
								<span
									class="badge badge-soft badge-sm badge-warning"
									title="{formatIsoDateTime(
										user.lockedUntil
									)} asti, liian monta epäonnistunutta kirjautumista"
								>
									Tilapäisesti lukittu
								</span>
							{/if}
						</td>
					</tr>
				{/each}
			</tbody>
		</table>

		<CompactPagination {list} />
	</div>
</div>
