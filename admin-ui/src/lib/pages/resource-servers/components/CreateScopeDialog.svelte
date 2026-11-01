<script lang="ts">
	import Dialog from '#lib/components/Dialog.svelte';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import { ScopeCreateModel } from '#lib/pages/scopes/ScopeCreateModel.svelte.ts';
	import Icon from '@iconify/svelte';

	interface Props {
		resourceServerId: string;
		onCreated: () => void;
		onCancel: () => void;
	}

	const { resourceServerId, onCreated, onCancel }: Props = $props();

	const model = new ScopeCreateModel();

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		model.resourceServerId = resourceServerId;
		const id = await model.submit();
		if (id) onCreated();
	}
</script>

<Dialog open={true}>
	<form class="modal-box" onsubmit={onSubmit}>
		<div class="flex items-center justify-between">
			<h3 class="text-lg font-semibold">Luo uusi scope</h3>
			<button
				type="button"
				class="btn btn-square btn-ghost btn-sm"
				aria-label="Sulje"
				disabled={model.submitting}
				onclick={onCancel}
			>
				<Icon icon="ic:baseline-close" width="20" />
			</button>
		</div>

		<GeneralErrors messages={[...model.generalErrors, ...model.errorsFor('resourceServerId')]} />

		<fieldset class="mt-4 fieldset gap-4" disabled={model.submitting}>
			<FormField>
				<FormLabel>Nimi</FormLabel>
				<TextInput
					bind:value={model.name}
					errors={model.errorsFor('name')}
					hint="Pienet kirjaimet, numerot sekä erottimet - : . (esim. orders:read)"
					required
				/>
			</FormField>

			<FormField>
				<FormLabel>Näytettävä nimi</FormLabel>
				<TextInput
					bind:value={model.displayName}
					errors={model.errorsFor('displayName')}
					required
				/>
			</FormField>
		</fieldset>

		<div class="modal-action">
			<button type="button" class="btn btn-ghost" disabled={model.submitting} onclick={onCancel}>
				Peruuta
			</button>
			<button type="submit" class="btn btn-primary" disabled={model.submitting}>
				{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
				Luo
			</button>
		</div>
	</form>
</Dialog>
