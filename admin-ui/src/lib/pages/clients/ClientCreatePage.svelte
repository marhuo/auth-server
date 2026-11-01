<script lang="ts">
	import { goto } from '$app/navigation';
	import type { ClientDtoTypes } from '#lib/api/generated/index.js';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import PasswordInput from '#lib/components/PasswordInput.svelte';
	import RadioGroup, { type RadioOption } from '#lib/components/RadioGroup.svelte';
	import ResourceServerCombobox from '#lib/components/ResourceServerCombobox.svelte';
	import StringListInput from '#lib/components/StringListInput.svelte';
	import TenantCombobox from '#lib/components/TenantCombobox.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import { clientTypeDescription, clientTypeLabel } from '#lib/domain/clientTypes.ts';
	import ScopeTags from '#lib/pages/clients/components/ScopeTags.svelte';
	import { ClientCreateModel } from '#lib/pages/clients/ClientCreateModel.svelte.ts';
	import { resolve } from '$app/paths';

	const model = new ClientCreateModel();

	const clientTypes: ClientDtoTypes[] = [
		'PublicApplication',
		'ConfidentialApplication',
		'ConfidentialMachine'
	];

	const clientTypeOptions: RadioOption[] = clientTypes.map((type) => ({
		value: type,
		label: clientTypeLabel(type),
		description: clientTypeDescription(type)
	}));

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		const id = await model.submit();
		if (id) goto(resolve('/clients/[id]', { id }), { replace: true });
	}
</script>

<svelte:head>
	<title>Uusi client</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a href={resolve('/clients')} class="link">Clientit</a></li>
		<li>Luo uusi</li>
	</ul>
</div>

<div class="card max-w-3xl border border-base-300">
	<form class="card-body" onsubmit={onSubmit}>
		<h1 class="mb-4 card-title">Uusi client</h1>

		<GeneralErrors messages={model.generalErrors} />

		<fieldset class="fieldset gap-6" disabled={model.submitting}>
			<FormField>
				<FormLabel>Tyyppi</FormLabel>
				<RadioGroup
					options={clientTypeOptions}
					bind:value={model.type}
					errors={model.errorsFor('type')}
					required
				/>
			</FormField>

			<FormField class="max-w-lg">
				<FormLabel>Client ID</FormLabel>
				<TextInput
					bind:value={model.clientId}
					errors={model.errorsFor('clientId')}
					hint="Kirjaimia, numeroita sekä . _ - (alussa kirjain tai numero)"
					required
				/>
			</FormField>

			{#if model.needsSecret}
				<FormField class="max-w-lg">
					<FormLabel>Salaisuus (client secret)</FormLabel>
					<PasswordInput
						bind:value={model.secret}
						errors={model.errorsFor('secret')}
						hint="Tallenna salaisuus talteen, sitä ei voi katsoa jälkikäteen"
						required
					/>
				</FormField>
			{/if}

			<FormField class="max-w-lg">
				<FormLabel>Nimi</FormLabel>
				<TextInput
					bind:value={model.displayName}
					errors={model.errorsFor('displayName')}
					required
				/>
			</FormField>

			<FormField class="max-w-lg">
				<FormLabel>Resurssipalvelin</FormLabel>
				<ResourceServerCombobox
					bind:value={model.resourceServerId}
					errors={model.errorsFor('resourceServerId')}
					required
					includeAuthServer={true}
				/>
			</FormField>

			<FormField class="max-w-lg">
				<FormLabel>Tenant</FormLabel>
				<TenantCombobox
					bind:value={model.tenantId}
					errors={model.errorsFor('tenantId')}
					hint="Jätä tyhjäksi, jos client ei kuulu tiettyyn tenanttiin"
				/>
			</FormField>

			{#if model.hasUsers}
				<div class="mt-2 flex flex-col gap-1 border-t border-base-300 pt-6">
					<h2 class="text-base font-semibold">Uudelleenohjaukset</h2>
					<p class="text-sm opacity-60">
						Osoitteet, joihin käyttäjä palautetaan kirjautumisen ja uloskirjautumisen jälkeen.
					</p>
				</div>

				<FormField class="max-w-lg">
					<FormLabel>Redirect URIt</FormLabel>
					<StringListInput
						bind:value={model.redirectUris}
						placeholder="https://example.com/callback"
						addLabel="Lisää redirect URI"
						errors={model.errorsFor('redirectUris')}
					/>
				</FormField>

				<FormField class="max-w-lg">
					<FormLabel>Uloskirjautumisen redirect URIt</FormLabel>
					<StringListInput
						bind:value={model.postLogoutRedirectUris}
						placeholder="https://example.com"
						addLabel="Lisää URI"
						errors={model.errorsFor('postLogoutRedirectUris')}
					/>
				</FormField>
			{/if}

			<div class="mt-2 flex flex-col gap-1 border-t border-base-300 pt-6">
				<h2 class="text-base font-semibold">Scopet</h2>
				<p class="text-sm opacity-60">
					Oikeudet, joita client voi pyytää. Valittavissa ovat globaalit scopet ja valitun
					resurssipalvelimen scopet.
				</p>
			</div>

			<ScopeTags
				bind:value={model.selectedScopes}
				options={model.selectableScopes}
				errors={model.errorsFor('scopeIds')}
				hint={model.resourceServerId ? undefined : 'Valitse ensin resurssipalvelin'}
				disabled={!model.resourceServerId}
				class="max-w-sm"
			/>
		</fieldset>

		<button type="submit" class="btn mt-6 self-start px-6 btn-primary" disabled={model.submitting}>
			{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
			Luo
		</button>
	</form>
</div>
