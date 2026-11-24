<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import ActionsMenu, { type MenuItem } from '#lib/components/ActionsMenu.svelte';
	import DeleteDialog from '#lib/components/DeleteDialog.svelte';
	import DetailsTextInput from '#lib/components/DetailsTextInput.svelte';
	import SuspendTenantDialog from '#lib/pages/tenants/components/SuspendTenantDialog.svelte';
	import UnsuspendTenantDialog from '#lib/pages/tenants/components/UnsuspendTenantDialog.svelte';
	import AddDomainDialog from '#lib/pages/tenants/components/AddDomainDialog.svelte';
	import VerifyDomainButton from '#lib/pages/tenants/components/VerifyDomainButton.svelte';
	import DeleteIconButton from '#lib/components/DeleteIconButton.svelte';
	import { TenantDetailsModel } from '#lib/pages/tenants/TenantDetailsModel.svelte.ts';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.ts';
	import { normalizeSlug } from '#lib/utils/normalizers.ts';
	import Icon from '@iconify/svelte';
	import { resolve } from '$app/paths';
	import { UserListModel } from '#lib/models/UserListModel.svelte.ts';
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import {
		userStatusBadgeClass,
		userStatusDescription,
		userStatusLabel
	} from '#lib/models/userStatuses.ts';

	const tenantId = page.params.id!;
	const model = new TenantDetailsModel(tenantId);
	model.load();

	const userList = new UserListModel(tenantId);
	userList.load();

	let suspendDialogOpen = $state(false);
	let unsuspendDialogOpen = $state(false);
	let deleteDialogOpen = $state(false);
	let addDomainDialogOpen = $state(false);

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

		if (model.canDelete) {
			items.push({
				title: 'Poista',
				icon: 'ic:outline-delete',
				onClick: () => (deleteDialogOpen = true)
			});
		}

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
	<div class="flex max-w-3xl flex-col gap-6">
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

					<li class="list-row">
						<div>
							<div class="text-xs opacity-60">Ensimmäinen kirjautuminen</div>
							{#if model.firstSignInAt}
								<div>{formatIsoDateTime(model.firstSignInAt)}</div>
							{:else}
								<div>
									<span class="badge badge-soft badge-sm badge-warning">Ei koskaan</span>
								</div>
							{/if}
						</div>
					</li>
				</ul>
			</div>
		</div>

		<div class="card max-w-3xl border border-base-300">
			<div class="card-body">
				<div class="flex items-center justify-between">
					<h2 class="card-title">Domainit</h2>
					<button class="btn btn-ghost btn-sm" onclick={() => (addDomainDialogOpen = true)}>
						<Icon icon="ic:outline-add" width="18" />
						Lisää domain
					</button>
				</div>
				<p class="text-sm opacity-60">
					Domain vahvistuu, kun sen DNS:ssä on TXT-rivi ja painetaan Tarkista.
				</p>

				<ul class="list -mx-4">
					{#each model.domains as domain (domain.id)}
						<li class="list-row items-center">
							<div class="list-col-grow">
								<div class="font-mono">{domain.name}</div>
								{#if !domain.verifiedAt}
									<div class="font-mono text-xs break-all opacity-60" title="TXT-rivi">
										{domain.txtRecord}
									</div>
									<div class="mt-2">
										<VerifyDomainButton onVerify={() => model.verifyDomain(domain.id)} />
									</div>
								{/if}
							</div>
							<div>
								{#if domain.verifiedAt}
									<span
										class="badge badge-soft badge-sm badge-success"
										title={formatIsoDateTime(domain.verifiedAt)}
									>
										Vahvistettu
									</span>
								{:else}
									<span class="badge badge-soft badge-sm badge-warning">Ei vahvistettu</span>
								{/if}
							</div>
							<DeleteIconButton
								dialogTitle="Poista domain"
								onDelete={() => model.deleteDomain(domain.id)}
							>
								Oletko varma, että haluat poistaa domainin <strong>{domain.name}</strong>?
							</DeleteIconButton>
						</li>
					{:else}
						<li class="list-row opacity-60">Ei domaineja</li>
					{/each}
				</ul>
			</div>
		</div>

		<div class="card max-w-3xl border border-base-300">
			<div class="card-body">
				<div class="mb-4 card-title">
					<h2>Käyttäjät</h2>
				</div>
				<div class="flex items-center justify-between gap-4">
					<label class="input w-full max-w-sm">
						<Icon icon="ic:outline-search" width="18" class="opacity-50" />
						<input
							type="search"
							placeholder="Hae"
							value={userList.searchTerm}
							oninput={(e) => userList.setSearchTerm(e.currentTarget.value)}
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
						{#each userList.items as user (user.id)}
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

				<CompactPagination list={userList} />
			</div>
		</div>
	</div>
	{#if deleteDialogOpen}
		<DeleteDialog
			dialogTitle="Poista tenant"
			onCancel={() => (deleteDialogOpen = false)}
			onDelete={model.delete}
			onDeleted={() => goto(resolve('/tenants'), { replace: true })}
		>
			Oletko varma, että haluat poistaa tenantin <strong>{model.name}</strong>? Tenantti, sen
			domainit ja käyttäjien roolit siinä poistetaan pysyvästi.
		</DeleteDialog>
	{/if}

	{#if addDomainDialogOpen}
		<AddDomainDialog onAdd={model.addDomain} onClose={() => (addDomainDialogOpen = false)} />
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
