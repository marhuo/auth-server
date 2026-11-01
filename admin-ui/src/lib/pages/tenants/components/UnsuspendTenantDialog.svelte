<script lang="ts">
	import { unsuspendTenant } from '#lib/api/tenantApi.js';
	import Dialog from '#lib/components/Dialog.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import { FormModelBase } from '#lib/models/FormModelBase.svelte.ts';
	import Icon from '@iconify/svelte';

	interface Props {
		tenantId: string;
		tenantName: string;
		onUnsuspended: () => void;
		onCancel: () => void;
	}

	const { tenantId, tenantName, onUnsuspended, onCancel }: Props = $props();

	class UnsuspendTenantModel extends FormModelBase {
		submit = (id: string) => this.run({ id }, (request) => unsuspendTenant(request.id));
	}

	const model = new UnsuspendTenantModel();

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		const result = await model.submit(tenantId);
		if (result !== null) onUnsuspended();
	}
</script>

<Dialog open={true}>
	<form class="modal-box max-w-sm" onsubmit={onSubmit}>
		<div class="flex items-center justify-between">
			<h3 class="text-lg font-semibold">Poista lukitus</h3>
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

		<GeneralErrors messages={model.errors} />

		<p class="py-4">Haluatko varmasti poistaa tenantin <strong>{tenantName}</strong> lukituksen?</p>

		<div class="modal-action">
			<button type="button" class="btn btn-ghost" disabled={model.submitting} onclick={onCancel}>
				Peruuta
			</button>
			<button type="submit" class="btn btn-primary" disabled={model.submitting}>
				{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
				Poista lukitus
			</button>
		</div>
	</form>
</Dialog>
