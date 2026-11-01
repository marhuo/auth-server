<script lang="ts">
	import { page } from '$app/state';
	import DeleteIconButton from '#lib/components/DeleteIconButton.svelte';
	import DetailsBooleanInput from '#lib/components/DetailsBooleanInput.svelte';
	import DetailsPasswordInput from '#lib/components/DetailsPasswordInput.svelte';
	import DetailsTextInput from '#lib/components/DetailsTextInput.svelte';
	import AddRoleDialog from '#lib/pages/users/components/AddRoleDialog.svelte';
	import { UserDetailsModel } from '#lib/pages/users/UserDetailsModel.svelte.ts';
	import { formatIsoDateTime } from '#lib/utils/dateUtils.ts';
	import Icon from '@iconify/svelte';
	import { resolve } from '$app/paths';

	const model = new UserDetailsModel(page.params.id!);
	model.load();

	let addRoleDialogOpen = $state(false);

	function onRoleAdded() {
		addRoleDialogOpen = false;
		model.load();
	}
</script>

<svelte:head>
	<title>{model.email || 'Käyttäjä'}</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a class="link" href={resolve('/users')}>Käyttäjät</a></li>
	</ul>
</div>

{#if model.loading && !model.email}
	<span class="loading loading-spinner"></span>
{:else if model.notFound}
	<div role="alert" class="alert alert-soft alert-warning">Käyttäjää ei löytynyt.</div>
{:else}
	<div class="flex max-w-3xl flex-col gap-6">
		<!-- Perustiedot -->
		<div class="card border border-base-300">
			<div class="card-body">
				<!-- Users are never deleted (access is removed by removing roles), so no actions menu. -->
				<h1 class="card-title">
					Käyttäjä <span class="text-accent">|</span>
					{model.email}
				</h1>

				<ul class="list -mx-4">
					<DetailsTextInput
						label="Sähköposti"
						type="email"
						value={model.email}
						onSave={model.changeEmail}
						required
					/>

					<DetailsPasswordInput
						label="Salasana"
						dialogTitle="Vaihda salasana"
						hasPassword={model.hasPassword}
						onSave={model.changePassword}
					/>

					<!-- The invited user has not set a password yet: the first email may have gone to spam. -->
					{#if !model.hasPassword}
						<li class="list-row items-center">
							<div class="list-col-grow">
								<div class="text-xs opacity-60">Kutsu</div>
								{#if model.inviteResent}
									<div>Lähetetty uudelleen</div>
								{:else}
									<div class="opacity-60">Odottaa salasanan asettamista</div>
								{/if}
								<div class="text-sm opacity-60">
									Uusi linkki on voimassa 7 päivää. Aiemmat linkit lakkaavat toimimasta.
								</div>
							</div>
							<button
								class="btn btn-ghost btn-sm"
								disabled={model.resendingInvite}
								onclick={model.resendInvite}
							>
								{#if model.resendingInvite}
									<span class="loading loading-sm loading-spinner"></span>
								{/if}
								Lähetä uudelleen
							</button>
						</li>
					{/if}

					<DetailsBooleanInput
						label="Lukittu"
						value={model.locked}
						hint="Lukittu käyttäjä ei voi kirjautua. Lukitus on voimassa, kunnes se poistetaan."
						onSave={model.changeLocked}
					/>

					<!-- Automatic lockout after too many failed logins: ends by itself, or an admin ends it
					     (saving locked = false also clears the lockout). -->
					{#if model.lockedOut && !model.locked}
						<li class="list-row items-center">
							<div class="list-col-grow">
								<div class="text-xs opacity-60">Tilapäinen lukitus</div>
								<div>{formatIsoDateTime(model.lockedUntil)} asti</div>
								<div class="text-sm opacity-60">Liian monta epäonnistunutta kirjautumista.</div>
							</div>
							<button class="btn btn-ghost btn-sm" onclick={() => model.changeLocked(false)}>
								Poista
							</button>
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
											<a href={resolve('/tenants/[id]', { id: ur.tenantId })} class="link">
												{ur.tenantName}
											</a>
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
			email={model.email}
			onAdded={onRoleAdded}
			onCancel={() => (addRoleDialogOpen = false)}
		/>
	{/if}
{/if}
