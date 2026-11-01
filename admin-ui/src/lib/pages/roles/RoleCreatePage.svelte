<script lang="ts">
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import ResourceServerCombobox from '#lib/components/ResourceServerCombobox.svelte';
	import TextAreaInput from '#lib/components/TextAreaInput.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import { RoleCreateModel } from '#lib/pages/roles/RoleCreateModel.svelte.ts';
	import { resolve } from '$app/paths';

	const model = new RoleCreateModel(page.url.searchParams.get('resourceServerId') ?? '');

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		const id = await model.submit();
		if (id) goto(resolve('/roles/[id]', { id }), { replace: true });
	}
</script>

<svelte:head>
	<title>Uusi rooli</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a href={resolve('/roles')} class="link">Roolit</a></li>
		<li>Luo uusi</li>
	</ul>
</div>

<div class="card max-w-3xl border border-base-300">
	<form class="card-body" onsubmit={onSubmit}>
		<h1 class="mb-4 card-title">Uusi rooli</h1>

		<GeneralErrors messages={model.generalErrors} />

		<fieldset class="fieldset gap-6" disabled={model.submitting}>
			<FormField class="max-w-lg">
				<FormLabel>Nimi</FormLabel>
				<TextInput
					bind:value={model.name}
					errors={model.errorsFor('name')}
					hint="Vain pieniä kirjaimia, numeroita ja yhdysmerkkejä"
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

			<FormField class="max-w-lg">
				<FormLabel>Kuvaus</FormLabel>
				<TextAreaInput
					bind:value={model.description}
					errors={model.errorsFor('description')}
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
