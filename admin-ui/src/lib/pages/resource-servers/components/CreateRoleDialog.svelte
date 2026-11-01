<script lang="ts">
	import Dialog from '#lib/components/Dialog.svelte';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import TextAreaInput from '#lib/components/TextAreaInput.svelte';
	import TextInput from '#lib/components/TextInput.svelte';
	import { RoleCreateModel } from '#lib/pages/roles/RoleCreateModel.svelte.ts';
	import Icon from '@iconify/svelte';

	interface Props {
		resourceServerId: string;
		onCreated: () => void;
		onCancel: () => void;
	}

	const { resourceServerId, onCreated, onCancel }: Props = $props();

	const model = new RoleCreateModel();

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
			<h3 class="text-lg font-semibold">Luo uusi rooli</h3>
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
					hint="Vain pieniä kirjaimia, numeroita ja yhdysmerkkejä"
					required
				/>
			</FormField>

			<FormField>
				<FormLabel>Kuvaus</FormLabel>
				<TextAreaInput
					bind:value={model.description}
					errors={model.errorsFor('description')}
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
