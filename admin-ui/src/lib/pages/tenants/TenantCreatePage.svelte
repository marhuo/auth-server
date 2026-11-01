<script lang="ts">
	import { goto } from '$app/navigation';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import { TenantCreateModel } from '#lib/pages/tenants/TenantCreateModel.svelte.ts';
	import { resolve } from '$app/paths';

	const model = new TenantCreateModel();

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		const id = await model.submit();
		if (id) goto(resolve('/tenants/[id]', { id }), { replace: true });
	}
</script>

<svelte:head>
	<title>Uusi tenant</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a class="link" href={resolve('/tenants')}>Tenantit</a></li>
		<li>Luo uusi</li>
	</ul>
</div>

<div class="card max-w-3xl border border-base-300">
	<form class="card-body" onsubmit={onSubmit}>
		<h1 class="mb-4 card-title">Uusi tenant</h1>

		<GeneralErrors messages={model.generalErrors} />

		<fieldset class="fieldset gap-6" disabled={model.submitting}>
			<FormField class="max-w-lg">
				<FormLabel>Nimi</FormLabel>
				<TextInput bind:value={model.name} errors={model.errorsFor('name')} required />
			</FormField>
		</fieldset>

		<button type="submit" class="btn mt-6 self-start px-6 btn-primary" disabled={model.submitting}>
			{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
			Luo
		</button>
	</form>
</div>
