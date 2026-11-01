<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import ActionsMenu, { type MenuItem } from '#lib/components/ActionsMenu.svelte';
	import DeleteDialog from '#lib/components/DeleteDialog.svelte';
	import DetailsResourceServerCombobox from '#lib/components/DetailsResourceServerCombobox.svelte';
	import DetailsTextAreaInput from '#lib/components/DetailsTextAreaInput.svelte';
	import DetailsTextInput from '#lib/components/DetailsTextInput.svelte';
	import { RoleDetailsModel } from '#lib/pages/roles/RoleDetailsModel.svelte.ts';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.ts';
	import { normalizeRoleName } from '#lib/utils/normalizers.ts';
	import { resolve } from '$app/paths';

	const model = new RoleDetailsModel(page.params.id!);
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
	<title>{model.name || 'Rooli'}</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a class="link" href={resolve('/roles')}>Roolit</a></li>
	</ul>
</div>

{#if model.loading}
	<span class="loading loading-spinner"></span>
{:else if model.notFound}
	<div role="alert" class="alert alert-soft alert-warning">Roolia ei löytynyt.</div>
{:else}
	<div class="card max-w-3xl border border-base-300">
		<div class="card-body">
			<div class="flex items-center gap-2">
				<h1 class="card-title flex-1">
					Rooli <span class="text-accent">|</span>
					{model.name}
				</h1>
				{#if !model.isAuthServer}
					<ActionsMenu items={menuItems} />
				{/if}
			</div>

			<ul class="list -mx-4">
				<DetailsTextInput
					label="Nimi"
					value={model.name}
					onSave={model.changeName}
					normalize={normalizeRoleName}
					hint="Vain pieniä kirjaimia, numeroita ja yhdysmerkkejä"
					editable={!model.isAuthServer}
					mono
					required
				/>

				<DetailsTextAreaInput
					label="Kuvaus"
					value={model.description}
					onSave={model.changeDescription}
					editable={!model.isAuthServer}
					required
				/>

				<DetailsResourceServerCombobox
					resourceServerId={model.resourceServerId}
					resourceServerName={model.resourceServerName}
					onSave={model.changeResourceServer}
					editable={!model.isAuthServer}
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

	{#if deleteDialogOpen}
		<DeleteDialog
			dialogTitle="Poista rooli"
			onCancel={() => (deleteDialogOpen = false)}
			onDelete={model.delete}
			onDeleted={() => goto('/roles', { replace: true })}
		>
			Oletko varma, että haluat poistaa roolin <strong>{model.name}</strong>?
		</DeleteDialog>
	{/if}
{/if}
