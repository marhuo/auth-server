<script lang="ts">
	import { page } from '$app/state';
	import { ClientDetailsModel } from '#lib/pages/clients/ClientDetailsModel.svelte.ts';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.js';
	import type { MenuItem } from '#lib/components/ActionsMenu.svelte';
	import DeleteDialog from '#lib/components/DeleteDialog.svelte';
	import { deleteClient } from '#lib/api/clientApi.ts';
	import { goto } from '$app/navigation';
	import ActionsMenu from '#lib/components/ActionsMenu.svelte';
	import Icon from '@iconify/svelte';
	import AddScopeDialog from '#lib/pages/clients/components/AddScopeDialog.svelte';
	import DeleteIconButton from '#lib/components/DeleteIconButton.svelte';
	import AddUriDialog from '#lib/pages/clients/components/AddUriDialog.svelte';
	import DetailsTextInput from '#lib/components/DetailsTextInput.svelte';
	import DetailsTenantCombobox from '#lib/components/DetailsTenantCombobox.svelte';
	import { resolve } from '$app/paths';

	const client = new ClientDetailsModel(page.params.id!);
	client.load();

	let deleteDialogOpen = $state(false);
	let addScopeDialogOpen = $state(false);
	let addRedirectUriDialogOpen = $state(false);
	let addLogoutRedirectUriDialogOpen = $state(false);
	const menuItems = $derived.by(() => {
		const items: MenuItem[] = [];
		items.push({
			title: 'Poista',
			icon: 'ic:outline-delete',
			onClick: () => {
				deleteDialogOpen = true;
			}
		});
		return items;
	});
</script>

<svelte:head>
	<title>{client.displayName || 'Client'}</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a class="link" href={resolve('/clients')}>Clientit</a></li>
	</ul>
</div>

{#if client.loading}
	<span class="loading loading-spinner"></span>
{:else if client.notFound}
	<div role="alert" class="alert alert-soft alert-warning">Clientia ei löytynyt.</div>
{:else}
	<div class="flex max-w-3xl flex-col gap-6">
		<!-- Perustiedot -->
		<div class="card border border-base-300">
			<div class="card-body">
				<div class="flex items-center gap-2">
					<h1 class="card-title flex-1">
						Client <span class="text-accent">|</span>
						{client.displayName}
					</h1>

					<ActionsMenu items={menuItems} />
				</div>
				<ul class="list -mx-4">
					<DetailsTextInput
						label="Client ID"
						value={client.clientId}
						mono={true}
						onSave={(value) => client.changeClientId(value)}
					/>

					<DetailsTextInput
						label="Nimi"
						value={client.displayName}
						onSave={(value) => client.changeDisplayName(value)}
					/>

					<li class="list-row">
						<div>
							<div class="text-xs opacity-60">Tyyppi</div>
							<div>{client.typeLabel}</div>
							{#if client.typeDescription}
								<div class="text-sm opacity-60">{client.typeDescription}</div>
							{/if}
						</div>
					</li>

					<li class="list-row">
						<div>
							<div class="text-xs opacity-60">Resurssipalvelin</div>
							<a
								href={resolve('/resource-servers/[id]', { id: client.resourceServerId })}
								class="link"
							>
								{client.resourceServerName}
							</a>
						</div>
					</li>

					<DetailsTenantCombobox
						label="Tenant"
						tenantId={client.tenantId}
						tenantName={client.tenantName}
						onSave={(tenant) => client.changeTenantId(tenant?.id ?? null)}
					/>

					<li class="list-row">
						<div>
							<div class="text-xs opacity-60">Sallitut grant-tyypit</div>
							<div class="mt-1 flex flex-wrap gap-1">
								{#each client.allowedGrantTypes as grantType (grantType)}
									<span class="badge badge-soft badge-sm">{grantType}</span>
								{/each}
							</div>
						</div>
					</li>

					<li class="list-row">
						<div>
							<div class="text-xs opacity-60">Luotu</div>
							<div>{formatIsoDateTime(client.createdAt)}</div>
						</div>
					</li>
				</ul>
			</div>
		</div>
		{#if client.hasUsers}
			<!-- Redirect URIt -->
			<div class="card border border-base-300">
				<div class="card-body">
					<div class="flex items-center gap-2">
						<h2 class="card-title flex-1">Redirect URIt</h2>
						<button class="btn btn-ghost btn-sm" onclick={() => (addRedirectUriDialogOpen = true)}>
							<Icon icon="ic:baseline-add" width="18" />
							Lisää
						</button>
					</div>
					{#if client.redirectUris.length > 0}
						<ul class="list -mx-4">
							{#each client.redirectUris as uri (uri)}
								<li class="list-row items-center">
									<div class="font-mono text-sm break-all list-col-grow">{uri}</div>
									<DeleteIconButton
										dialogTitle="Poista redirect URI"
										onDelete={() => client.removeRedirectUri(uri)}
									>
										Oletko varma, että haluat poistaa redirect URIn <strong class="break-all"
											>{uri}</strong
										>?
									</DeleteIconButton>
								</li>
							{/each}
						</ul>
					{:else}
						<p class="text-sm opacity-60">Ei redirect URIeja.</p>
					{/if}
				</div>
			</div>

			<!-- Uloskirjautumisen redirect URIt -->
			<div class="card border border-base-300">
				<div class="card-body">
					<div class="flex items-center gap-2">
						<h2 class="card-title flex-1">Uloskirjautumisen redirect URIt</h2>
						<button
							class="btn btn-ghost btn-sm"
							onclick={() => (addLogoutRedirectUriDialogOpen = true)}
						>
							<Icon icon="ic:baseline-add" width="18" />
							Lisää
						</button>
					</div>
					{#if client.postLogoutRedirectUris.length > 0}
						<ul class="list -mx-4">
							{#each client.postLogoutRedirectUris as uri (uri)}
								<li class="list-row items-center">
									<div class="font-mono text-sm break-all list-col-grow">{uri}</div>
									<DeleteIconButton
										dialogTitle="Poista uloskirjautumisen redirect URI"
										onDelete={() => client.removePostLogoutRedirectUri(uri)}
									>
										Oletko varma, että haluat poistaa URIn <strong class="break-all">{uri}</strong>?
									</DeleteIconButton>
								</li>
							{/each}
						</ul>
					{:else}
						<p class="text-sm opacity-60">Ei uloskirjautumisen redirect URIeja.</p>
					{/if}
				</div>
			</div>
		{/if}
		<!-- Scopet -->
		<div class="card border border-base-300">
			<div class="card-body">
				<div class="flex items-center gap-2">
					<h2 class="card-title flex-1">Scopet</h2>
					<button class="btn btn-ghost btn-sm" onclick={() => (addScopeDialogOpen = true)}>
						<Icon icon="ic:baseline-add" width="18" />
						Lisää
					</button>
				</div>

				{#if client.scopes.length > 0}
					<ul class="list -mx-4">
						{#each client.scopes as scope (scope.id)}
							<li class="list-row items-center">
								<div class="list-col-grow">
									<div class="flex items-center gap-2">
										<a href={resolve('/scopes/[id]', { id: scope.id })} class="link">
											{scope.name}
										</a>
										{#if scope.resourceServerId === null}
											<span class="badge badge-soft badge-sm badge-accent">Globaali</span>
										{/if}
									</div>
									<div class="text-sm opacity-60">{scope.displayName}</div>
								</div>

								<DeleteIconButton
									dialogTitle="Poista scope"
									onDelete={() => client.removeScope(scope.id)}
								>
									Oletko varma, että haluat poistaa scopen <strong>{scope.name}</strong>?
								</DeleteIconButton>
							</li>
						{/each}
					</ul>
				{:else}
					<p class="text-sm opacity-60">Ei scopeja.</p>
				{/if}
			</div>
		</div>
	</div>

	{#if deleteDialogOpen}
		{const clientId = client.id}
		<DeleteDialog
			dialogTitle="Poista"
			onCancel={() => (deleteDialogOpen = false)}
			onDelete={() => deleteClient(clientId)}
			onDeleted={() => goto('/clients', { replace: true })}
		>
			Oletko varma, että haluat poistaa clientin <strong>{client.displayName}</strong>
		</DeleteDialog>
	{/if}

	{#if addScopeDialogOpen}
		{const clientType = client.type}
		{const resourceServerId = client.resourceServerId}
		<AddScopeDialog
			{clientType}
			{resourceServerId}
			selected={client.scopes}
			onAdd={(scope) => client.addScope(scope)}
			onClose={() => (addScopeDialogOpen = false)}
		/>
	{/if}
	{#if addRedirectUriDialogOpen}
		<AddUriDialog
			title="Lisää redirect URI"
			label="Redirect URI"
			placeholder="https://example.com/callback"
			onAdd={client.addRedirectUri}
			onClose={() => (addRedirectUriDialogOpen = false)}
		/>
	{/if}

	{#if addLogoutRedirectUriDialogOpen}
		<AddUriDialog
			title="Lisää uloskirjautumisen redirect URI"
			label="Uloskirjautumisen redirect URI"
			placeholder="https://example.com"
			onAdd={client.addPostLogoutRedirectUri}
			onClose={() => (addLogoutRedirectUriDialogOpen = false)}
		/>
	{/if}
{/if}
