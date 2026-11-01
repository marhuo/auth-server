<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { getRolePagedList } from '#lib/api/roleApi.js';
	import { getScopePagedList } from '#lib/api/scopeApi.js';
	import ActionsMenu, { type MenuItem } from '#lib/components/ActionsMenu.svelte';
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import DeleteDialog from '#lib/components/DeleteDialog.svelte';
	import DetailsTextInput from '#lib/components/DetailsTextInput.svelte';
	import CreateRoleDialog from '#lib/pages/resource-servers/components/CreateRoleDialog.svelte';
	import CreateScopeDialog from '#lib/pages/resource-servers/components/CreateScopeDialog.svelte';
	import { PagedList } from '#lib/models/PagedList.svelte.js';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.ts';
	import Icon from '@iconify/svelte';
	import { ResourceServerDetailsModel } from '#lib/pages/resource-servers/ResourceServerDetailsModel.svelte.ts';
	import { resolve } from '$app/paths';

	const id = page.params.id!;

	const model = new ResourceServerDetailsModel(id);
	const roles = new PagedList(getRolePagedList, { resourceServerId: id });
	const scopes = new PagedList(getScopePagedList, { resourceServerId: id });

	model.load();
	roles.load();
	scopes.load();

	let createRoleDialogOpen = $state(false);
	let createScopeDialogOpen = $state(false);
	let deleteDialogOpen = $state(false);

	function onRoleCreated() {
		createRoleDialogOpen = false;
		roles.setPageNumber(1);
	}

	function onScopeCreated() {
		createScopeDialogOpen = false;
		scopes.setPageNumber(1);
	}

	const menuItems: MenuItem[] = [
		{
			title: 'Poista',
			icon: 'ic:outline-delete',
			onClick: () => (deleteDialogOpen = true)
		}
	];
</script>

<svelte:head>
	<title>{model.displayName || 'Resurssipalvelin'}</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a class="link" href={resolve('/resource-servers')}>Resurssipalvelimet</a></li>
	</ul>
</div>

{#if model.loading}
	<span class="loading loading-spinner"></span>
{:else if model.notFound}
	<div role="alert" class="alert alert-soft alert-warning">Resurssipalvelinta ei löytynyt.</div>
{:else}
	<div class="flex max-w-3xl flex-col gap-6">
		<!-- Perustiedot -->
		<div class="card border border-base-300">
			<div class="card-body">
				<div class="flex items-center gap-2">
					<h1 class="card-title flex-1">
						Resurssipalvelin <span class="text-accent">|</span>
						{model.displayName}
					</h1>
					{#if !model.isAuthServer}
						<ActionsMenu items={menuItems} />
					{/if}
				</div>

				<ul class="list -mx-4">
					<DetailsTextInput
						label="Näytettävä nimi"
						value={model.displayName}
						onSave={model.changeDisplayName}
						editable={!model.isAuthServer}
						required
					/>
					<DetailsTextInput
						label="Tunniste (audience)"
						value={model.audience}
						onSave={model.changeAudience}
						editable={!model.isAuthServer}
						mono
						required
					/>
					<li class="list-row">
						<div>
							<div class="text-xs opacity-60">Luotu</div>
							<div>{formatIsoDateTime(model.createdAt)}</div>
						</div>
					</li>
				</ul>
			</div>
		</div>

		<!-- Roolit -->
		<div class="card border border-base-300">
			<div class="card-body">
				<div class="flex items-center gap-2">
					<h2 class="card-title flex-1">Roolit</h2>
					{#if !model.isAuthServer}
						<button class="btn btn-ghost btn-sm" onclick={() => (createRoleDialogOpen = true)}>
							<Icon icon="ic:baseline-add" width="18" />
							Lisää uusi
						</button>
					{/if}
				</div>

				{#if roles.items.length > 0}
					<table class="table">
						<thead>
							<tr>
								<th>Nimi</th>
								<th>Kuvaus</th>
							</tr>
						</thead>
						<tbody>
							{#each roles.items as role (role.id)}
								<tr class="hover:bg-base-200">
									<td
										><a href={resolve('/roles/[id]', { id: role.id })} class="link">{role.name}</a
										></td
									>
									<td>{role.description}</td>
								</tr>
							{/each}
						</tbody>
					</table>

					<CompactPagination list={roles} />
				{/if}
			</div>
		</div>

		<!-- Scopet -->
		<div class="card border border-base-300">
			<div class="card-body">
				<div class="flex items-center gap-2">
					<h2 class="card-title flex-1">Scopet</h2>
					{#if !model.isAuthServer}
						<button class="btn btn-ghost btn-sm" onclick={() => (createScopeDialogOpen = true)}>
							<Icon icon="ic:baseline-add" width="18" />
							Lisää uusi
						</button>
					{/if}
				</div>

				{#if scopes.items.length > 0}
					<table class="table">
						<thead>
							<tr>
								<th>Nimi</th>
								<th>Näytettävä nimi</th>
							</tr>
						</thead>
						<tbody>
							{#each scopes.items as scope (scope.id)}
								<tr class="hover:bg-base-200">
									<td>
										<a href={resolve('/scopes/[id]', { id: scope.id })} class="link">
											{scope.name}
										</a>
									</td>
									<td>{scope.displayName}</td>
								</tr>
							{/each}
						</tbody>
					</table>

					<CompactPagination list={scopes} />
				{/if}
			</div>
		</div>
	</div>

	{#if deleteDialogOpen}
		<DeleteDialog
			dialogTitle="Poista resurssipalvelin"
			onCancel={() => (deleteDialogOpen = false)}
			onDelete={model.delete}
			onDeleted={() => goto('/resource-servers', { replace: true })}
		>
			Oletko varma, että haluat poistaa resurssipalvelimen <strong>{model.displayName}</strong>?
		</DeleteDialog>
	{/if}

	{#if createRoleDialogOpen}
		<CreateRoleDialog
			resourceServerId={model.id}
			onCreated={onRoleCreated}
			onCancel={() => (createRoleDialogOpen = false)}
		/>
	{/if}

	{#if createScopeDialogOpen}
		<CreateScopeDialog
			resourceServerId={model.id}
			onCreated={onScopeCreated}
			onCancel={() => (createScopeDialogOpen = false)}
		/>
	{/if}
{/if}
