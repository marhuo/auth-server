<script lang="ts">
	import { suspendTenant } from '#lib/api/tenantApi.js';
	import Dialog from '#lib/components/Dialog.svelte';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import TextAreaInput from '#lib/components/TextAreaInput.svelte';
	import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';
	import Icon from '@iconify/svelte';

	interface Props {
		tenantId: string;
		tenantName: string;
		onSuspended: () => void;
		onCancel: () => void;
	}

	const { tenantId, tenantName, onSuspended, onCancel }: Props = $props();

	class SuspendTenantModel extends FormModelBase {
		suspensionReason = $state('');

		submit = (id: string) =>
			this.run({ id, suspensionReason: this.suspensionReason }, suspendTenant);
	}

	const model = new SuspendTenantModel();

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		const result = await model.submit(tenantId);
		if (result !== null) onSuspended();
	}
</script>

<Dialog open={true}>
	<form class="modal-box" onsubmit={onSubmit}>
		<div class="flex items-center justify-between">
			<h3 class="text-lg font-semibold">Lukitse tenant: {tenantName}</h3>
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

		<GeneralErrors messages={model.generalErrors} />

		<fieldset class="mt-4 fieldset" disabled={model.submitting}>
			<FormField>
				<FormLabel>Anna syy lukitukselle</FormLabel>
				<TextAreaInput
					bind:value={model.suspensionReason}
					errors={model.errorsFor('suspensionReason')}
					required
				/>
			</FormField>
		</fieldset>

		<div class="modal-action">
			<button type="button" class="btn btn-ghost" disabled={model.submitting} onclick={onCancel}>
				Peruuta
			</button>
			<button type="submit" class="btn btn-error" disabled={model.submitting}>
				{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
				Lukitse
			</button>
		</div>
	</form>
</Dialog>
