<script lang="ts">
	import { goto } from '$app/navigation';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import PasswordInput from '#lib/components/PasswordInput.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import { UserCreateModel } from '#lib/pages/users/UserCreateModel.svelte.ts';
	import { resolve } from '$app/paths';

	const model = new UserCreateModel();

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		const id = await model.submit();
		if (id) goto(resolve('/users/[id]', { id }), { replace: true });
	}
</script>

<svelte:head>
	<title>Uusi käyttäjä</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a href={resolve('/users')} class="link">Käyttäjät</a></li>
		<li>Luo uusi</li>
	</ul>
</div>

<div class="card max-w-3xl border border-base-300">
	<form class="card-body" onsubmit={onSubmit}>
		<h1 class="mb-4 card-title">Uusi käyttäjä</h1>

		<GeneralErrors messages={model.generalErrors} />

		<fieldset class="fieldset gap-6" disabled={model.submitting}>
			<FormField class="max-w-lg">
				<FormLabel>Käyttäjänimi</FormLabel>
				<TextInput bind:value={model.userName} errors={model.errorsFor('userName')} required />
			</FormField>

			<FormField class="max-w-lg">
				<FormLabel>Sähköposti</FormLabel>
				<TextInput
					type="email"
					bind:value={model.email}
					errors={model.errorsFor('email')}
					required
				/>
			</FormField>

			<FormField class="max-w-lg">
				<FormLabel>Salasana</FormLabel>
				<PasswordInput bind:value={model.password} errors={model.errorsFor('password')} required />
			</FormField>
		</fieldset>

		<button type="submit" class="btn mt-6 self-start px-6 btn-primary" disabled={model.submitting}>
			{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
			Luo
		</button>
	</form>
</div>
