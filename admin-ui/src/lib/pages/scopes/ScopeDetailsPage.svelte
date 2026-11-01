<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import ActionsMenu, { type MenuItem } from '#lib/components/ActionsMenu.svelte';
	import DeleteDialog from '#lib/components/DeleteDialog.svelte';
	import DetailsTextInput from '#lib/components/DetailsTextInput.svelte';
	import { ScopeDetailsModel } from '#lib/pages/scopes/ScopeDetailsModel.svelte.ts';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.ts';
	import { normalizeScopeName } from '#lib/utils/normalizers.ts';
	import Icon from '@iconify/svelte';
	import { resolve } from '$app/paths';
	import { resolve4 } from 'node:dns/promises';

	const model = new ScopeDetailsModel(page.params.id!);
	model.load();

	let deleteDialogOpen = $state(false);

	const menuItems: MenuItem[] = [
		{
			title: 'Poista',
			icon: 'ic:outline-delete',
			onClick: () => (deleteDialogOpen = true)
		}
	];
</script>

<svelte:head>
	<title>{model.displayName || 'Scope'}</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a class="link" href={resolve('/scopes')}>Scopet</a></li>
	</ul>
</div>

{#if model.loading}
	<span class="loading loading-spinner"></span>
{:else if model.notFound}
	<div role="alert" class="alert alert-soft alert-warning">Scopea ei löytynyt.</div>
{:else}
	<div class="card max-w-3xl border border-base-300">
		<div class="card-body">
			<div class="flex items-center gap-2">
				<h1 class="card-title flex-1">
					Scope <span class="text-accent">|</span>
					{model.name}
				</h1>
				{#if !model.isAuthServer && !model.isStandard}
					<ActionsMenu items={menuItems} />
				{/if}
			</div>

			<ul class="list -mx-4">
				<DetailsTextInput
					label="Nimi"
					value={model.name}
					onSave={model.changeName}
					normalize={normalizeScopeName}
					hint="Pienet kirjaimet, numerot sekä erottimet - : . (esim. orders:read)"
					editable={!model.isAuthServer && !model.isStandard}
					mono
					required
				/>

				<DetailsTextInput
					label="Näytettävä nimi"
					value={model.displayName}
					onSave={model.changeDisplayName}
					editable={!model.isAuthServer}
					required
				/>

				<li class="list-row">
					<div>
						<div class="text-xs opacity-60">Resurssipalvelin</div>
						{#if model.isGlobal}
							<div class="flex items-center gap-1">
								<Icon icon="ic:outline-public" width="16" />
								Globaali scope
							</div>
						{:else if model.resourceServerId !== null}
							<a
								href={resolve('/resource-servers/[id]', { id: model.resourceServerId })}
								class="link"
							>
								{model.resourceServerName}
							</a>
						{/if}
					</div>
				</li>

				<li class="list-row">
					<div>
						<div class="text-xs opacity-60">Luotu</div>
						<div>{formatIsoDateTime(model.createdAt)}</div>
					</div>
				</li>
			</ul>
		</div>
	</div>

	{#if deleteDialogOpen}
		<DeleteDialog
			dialogTitle="Poista scope"
			onCancel={() => (deleteDialogOpen = false)}
			onDelete={model.delete}
			onDeleted={() => goto(resolve('/scopes'), { replace: true })}
		>
			Oletko varma, että haluat poistaa scopen <strong>{model.name}</strong>?
		</DeleteDialog>
	{/if}
{/if}
