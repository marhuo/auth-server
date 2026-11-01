<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import ResourceServerCombobox from '#lib/components/ResourceServerCombobox.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import { ScopeCreateModel } from '#lib/pages/scopes/ScopeCreateModel.svelte.ts';
	import { resolve } from '$app/paths';

	// Valinnainen esivalinta osoitteesta, esim. /scopes/create?resourceServerId=abc
	const model = new ScopeCreateModel(page.url.searchParams.get('resourceServerId') ?? '');

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		const id = await model.submit();
		if (id) goto(resolve('/scopes/[id]', { id }), { replace: true });
	}
</script>

<svelte:head>
	<title>Uusi scope</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a href={resolve('/scopes')} class="link">Scopet</a></li>
		<li>Luo uusi</li>
	</ul>
</div>

<div class="card max-w-3xl border border-base-300">
	<form class="card-body" onsubmit={onSubmit}>
		<h1 class="mb-4 card-title">Uusi scope</h1>

		<GeneralErrors messages={model.generalErrors} />

		<fieldset class="fieldset gap-6" disabled={model.submitting}>
			<FormField class="max-w-lg">
				<FormLabel>Nimi</FormLabel>
				<TextInput
					bind:value={model.name}
					errors={model.errorsFor('name')}
					hint="Pienet kirjaimet, numerot sekä erottimet - : . (esim. orders:read)"
					required
				/>
			</FormField>

			<FormField class="max-w-lg">
				<FormLabel>Näytettävä nimi</FormLabel>
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
				/>
			</FormField>
		</fieldset>

		<button type="submit" class="btn mt-6 self-start px-6 btn-primary" disabled={model.submitting}>
			{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
			Luo
		</button>
	</form>
</div>
