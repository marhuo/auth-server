<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import { UserInviteModel } from '#lib/pages/users/UserInviteModel.svelte.ts';

	const model = new UserInviteModel();

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		const userId = await model.submit();
		if (userId) goto(resolve('/users/[id]', { id: userId }), { replace: true });
	}
</script>

<svelte:head>
	<title>Kutsu käyttäjä</title>
</svelte:head>

<div class="breadcrumbs text-sm">
	<ul>
		<li><a href={resolve('/users')} class="link">Käyttäjät</a></li>
		<li>Kutsu käyttäjä</li>
	</ul>
</div>

<div class="card max-w-3xl border border-base-300">
	<form class="card-body" onsubmit={onSubmit}>
		<h1 class="mb-4 card-title">Kutsu käyttäjä</h1>

		<p class="mb-2 text-sm opacity-60">
			Käyttäjä luodaan ja hänelle lähetetään kutsu. Hän kirjautuu sähköpostiin tulevalla koodilla.
			Roolit lisätään käyttäjän tiedoissa kutsun jälkeen.
		</p>

		<GeneralErrors messages={model.generalErrors} />

		<fieldset class="fieldset gap-6" disabled={model.submitting}>
			<FormField class="max-w-lg">
				<FormLabel>Sähköposti</FormLabel>
				<TextInput
					type="email"
					bind:value={model.email}
					errors={model.errorsFor('email')}
					required
				/>
			</FormField>
		</fieldset>

		<button type="submit" class="btn mt-6 self-start px-6 btn-primary" disabled={model.submitting}>
			{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
			Lähetä kutsu
		</button>
	</form>
</div>
