<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import ActionsMenu, { type MenuItem } from '#lib/components/ActionsMenu.svelte';
	import DeleteDialog from '#lib/components/DeleteDialog.svelte';
	import DeleteIconButton from '#lib/components/DeleteIconButton.svelte';
	import DetailsDateTimeInput from '#lib/components/DetailsDateTimeInput.svelte';
	import DetailsTextInput from '#lib/components/DetailsTextInput.svelte';
	import AddRoleDialog from '#lib/pages/users/components/AddRoleDialog.svelte';
	import ChangePasswordDialog from '#lib/pages/users/components/ChangePasswordDialog.svelte';
	import { UserDetailsModel } from '#lib/pages/users/UserDetailsModel.svelte.ts';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.ts';
	import Icon from '@iconify/svelte';
	import { resolve } from '$app/paths';

	const model = new UserDetailsModel(page.params.id!);
	model.load();

	let addRoleDialogOpen = $state(false);
	let changePasswordDialogOpen = $state(false);
	let deleteDialogOpen = $state(false);

	const menuItems: MenuItem[] = [
		{
			title: 'Vaihda salasana',
			icon: 'ic:outline-lock-reset',
			onClick: () => (changePasswordDialogOpen = true)
		},
		{
			title: 'Poista',
			icon: 'ic:outline-delete',
			onClick: () => (deleteDialogOpen = true)
		}
	];

	function onRoleAdded() {
		addRoleDialogOpen = false;
		model.load();
	}

	const now = new Date();
	const maxDate = new Date(now.getFullYear() + 100, now.getMonth(), now.getDate());
</script>

<svelte:head>
	<title>{model.userName || 'Käyttäjä'}</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a class="link" href={resolve('/users')}>Käyttäjät</a></li>
	</ul>
</div>

{#if model.loading && !model.userName}
	<span class="loading loading-spinner"></span>
{:else if model.notFound}
	<div role="alert" class="alert alert-soft alert-warning">Käyttäjää ei löytynyt.</div>
{:else}
	<div class="flex max-w-3xl flex-col gap-6">
		<!-- Perustiedot -->
		<div class="card border border-base-300">
			<div class="card-body">
				<div class="flex items-center gap-2">
					<h1 class="card-title flex-1">
						Käyttäjä <span class="text-accent">|</span>
						{model.userName}
					</h1>
					<ActionsMenu items={menuItems} />
				</div>

				<ul class="list -mx-4">
					<DetailsTextInput
						label="Käyttäjänimi"
						value={model.userName}
						onSave={model.changeUserName}
						required
					/>

					<DetailsTextInput
						label="Sähköposti"
						type="email"
						value={model.email}
						onSave={model.changeEmail}
						required
					/>

					<DetailsDateTimeInput
						label="Lukittu asti"
						value={model.lockedUntil}
						emptyText="Ei lukittu"
						min={now}
						max={maxDate}
						hint="Tyhjennä poistaaksesi lukituksen"
						onSave={model.changeLockedUntil}
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
					<button class="btn btn-ghost btn-sm" onclick={() => (addRoleDialogOpen = true)}>
						<Icon icon="ic:baseline-add" width="18" />
						Lisää rooli
					</button>
				</div>

				{#if model.userRoles.length > 0}
					<table class="table">
						<thead>
							<tr>
								<th>Nimi</th>
								<th>Resurssipalvelin</th>
								<th>Tenant</th>
								<th></th>
							</tr>
						</thead>
						<tbody>
							{#each model.userRoles as ur, index (index)}
								<tr class="hover:bg-base-200">
									<td>
										<a
											href={resolve('/roles/[id]', { id: ur.roleId })}
											class="link"
											title={ur.roleDescription}
										>
											{ur.roleName}
										</a>
									</td>
									<td>
										<a
											href={resolve('/resource-servers/[id]', { id: ur.resourceServerId })}
											class="link"
										>
											{ur.resourceServerName}
										</a>
									</td>
									<td>
										{#if ur.tenantId}
											<a href={resolve('/tenants/[id]', { id: ur.tenantId })} class="link"
												>{ur.tenantName}</a
											>
										{/if}
									</td>
									<td class="text-right">
										<DeleteIconButton
											dialogTitle="Poista rooli"
											onDelete={() => model.removeRole(ur.roleId, ur.tenantId)}
										>
											Oletko varma, että haluat poistaa roolin <strong>{ur.roleName}</strong>?
										</DeleteIconButton>
									</td>
								</tr>
							{/each}
						</tbody>
					</table>
				{:else}
					<p class="text-sm opacity-60">Ei rooleja.</p>
				{/if}
			</div>
		</div>
	</div>

	{#if addRoleDialogOpen}
		<AddRoleDialog
			userId={model.id}
			username={model.userName}
			onAdded={onRoleAdded}
			onCancel={() => (addRoleDialogOpen = false)}
		/>
	{/if}

	{#if changePasswordDialogOpen}
		<ChangePasswordDialog
			userId={model.id}
			username={model.userName}
			onChanged={() => (changePasswordDialogOpen = false)}
			onCancel={() => (changePasswordDialogOpen = false)}
		/>
	{/if}

	{#if deleteDialogOpen}
		<DeleteDialog
			dialogTitle="Poista käyttäjä"
			onCancel={() => (deleteDialogOpen = false)}
			onDelete={model.delete}
			onDeleted={() => goto(resolve('/users'), { replace: true })}
		>
			Oletko varma, että haluat poistaa käyttäjän <strong>{model.userName}</strong>?
		</DeleteDialog>
	{/if}
{/if}
