<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import ActionsMenu, { type MenuItem } from '#lib/components/ActionsMenu.svelte';
	import DeleteDialog from '#lib/components/DeleteDialog.svelte';
	import DetailsTextInput from '#lib/components/DetailsTextInput.svelte';
	import SuspendTenantDialog from '#lib/pages/tenants/components/SuspendTenantDialog.svelte';
	import UnsuspendTenantDialog from '#lib/pages/tenants/components/UnsuspendTenantDialog.svelte';
	import { TenantDetailsModel } from '#lib/pages/tenants/TenantDetailsModel.svelte.ts';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.ts';
	import { normalizeSlug } from '#lib/utils/normalizers.ts';
	import Icon from '@iconify/svelte';
	import { resolve } from '$app/paths';

	const model = new TenantDetailsModel(page.params.id!);
	model.load();

	let suspendDialogOpen = $state(false);
	let unsuspendDialogOpen = $state(false);
	let deleteDialogOpen = $state(false);

	const menuItems = $derived.by(() => {
		const items: MenuItem[] = [];

		if (model.isSuspended) {
			items.push({
				title: 'Poista lukitus',
				icon: 'ic:outline-lock-open',
				onClick: () => (unsuspendDialogOpen = true)
			});
		} else {
			items.push({
				title: 'Lukitse',
				icon: 'ic:outline-lock',
				onClick: () => (suspendDialogOpen = true)
			});
		}

		items.push({
			title: 'Poista',
			icon: 'ic:outline-delete',
			onClick: () => (deleteDialogOpen = true)
		});

		return items;
	});
</script>

<svelte:head>
	<title>{model.name || 'Tenant'}</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a class="link" href={resolve('/tenants')}>Tenantit</a></li>
	</ul>
</div>

{#if model.loading}
	<span class="loading loading-spinner"></span>
{:else if model.notFound}
	<div role="alert" class="alert alert-soft alert-warning">Tenanttia ei löytynyt.</div>
{:else}
	<div class="card max-w-3xl border border-base-300">
		<div class="card-body">
			<div class="flex items-center gap-2">
				<h1 class="card-title flex-1">
					Tenant <span class="text-accent">|</span>
					{model.name}
				</h1>
				<ActionsMenu items={menuItems} />
			</div>

			<ul class="list -mx-4">
				<DetailsTextInput label="Nimi" value={model.name} onSave={model.changeName} required />

				<DetailsTextInput
					label="URL-tunniste (slug)"
					value={model.slug}
					onSave={model.changeSlug}
					normalize={normalizeSlug}
					hint="Vain pieniä kirjaimia, numeroita ja yhdysmerkkejä"
					mono
					required
				/>

				{#if model.isSuspended}
					<li class="list-row">
						<div>
							<div class="flex items-center gap-1 text-xs opacity-60">
								<Icon icon="ic:outline-lock" width="14" class="text-error" />
								Lukittu
							</div>
							<div>{formatIsoDateTime(model.suspendedAt)}</div>
							<div>{model.suspensionReason}</div>
						</div>
					</li>
				{/if}

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
			dialogTitle="Poista tenant"
			onCancel={() => (deleteDialogOpen = false)}
			onDelete={model.delete}
			onDeleted={() => goto(resolve('/tenants'), { replace: true })}
		>
			Oletko varma, että haluat poistaa tenantin <strong>{model.name}</strong>?
		</DeleteDialog>
	{/if}

	{#if suspendDialogOpen}
		<SuspendTenantDialog
			tenantId={model.id}
			tenantName={model.name}
			onSuspended={() => {
				suspendDialogOpen = false;
				model.load();
			}}
			onCancel={() => (suspendDialogOpen = false)}
		/>
	{/if}

	{#if unsuspendDialogOpen}
		<UnsuspendTenantDialog
			tenantId={model.id}
			tenantName={model.name}
			onUnsuspended={() => {
				unsuspendDialogOpen = false;
				model.load();
			}}
			onCancel={() => (unsuspendDialogOpen = false)}
		/>
	{/if}
{/if}
