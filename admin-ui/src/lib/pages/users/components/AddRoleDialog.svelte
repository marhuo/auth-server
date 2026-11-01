<script lang="ts">
	import type { RoleDto, TenantDto } from '#lib/api/generated/index.ts';
	import { getRolePagedList } from '#lib/api/roleApi.ts';
	import { getTenantPagedList } from '#lib/api/tenantApi.ts';
	import AutocompleteCombobox, {
		type SelectOption
	} from '#lib/components/AutocompleteCombobox.svelte';
	import Dialog from '#lib/components/Dialog.svelte';
	import FormField from '#lib/components/FormField.svelte';
	import FormLabel from '#lib/components/FormLabel.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import { AddRoleModel } from '#lib/pages/users/components/AddRoleModel.svelte.ts';
	import Icon from '@iconify/svelte';

	interface Props {
		userId: string;
		username: string;
		onAdded: () => void;
		onCancel: () => void;
	}

	let { userId, username, onAdded, onCancel }: Props = $props();
	const model = new AddRoleModel();

	let roleResults: RoleDto[] = [];
	let tenantResults: TenantDto[] = [];

	async function searchRoles(text: string): Promise<SelectOption[]> {
		const result = await getRolePagedList({
			pageNumber: 1,
			pageSize: 10,
			searchTerm: text || null,
			resourceServerId: null
		});
		roleResults = result.items;
		return roleResults.map((r) => ({ value: r.id, label: r.name }));
	}

	async function searchTenants(text: string): Promise<SelectOption[]> {
		const result = await getTenantPagedList({
			pageNumber: 1,
			pageSize: 10,
			searchTerm: text || null
		});
		tenantResults = result.items;
		return tenantResults.map((t) => ({ value: t.id, label: t.name }));
	}

	async function onAddClick(e: SubmitEvent) {
		e.preventDefault();
		const result = await model.submit(userId);
		if (result !== null) onAdded();
	}
</script>

<Dialog open={true}>
	<form class="modal-box flex min-h-96 flex-col gap-6 overflow-visible" onsubmit={onAddClick}>
		<div class="flex items-center justify-between">
			<h3 class="text-lg font-semibold">Lisää rooli käyttäjälle: {username}</h3>
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

		<fieldset class="fieldset gap-6" disabled={model.submitting}>
			<FormField class="max-w-lg">
				<FormLabel>Rooli</FormLabel>
				<AutocompleteCombobox
					bind:value={
						() => (model.role ? { value: model.role.id, label: model.role.name } : null),
						(option) => (model.role = roleResults.find((r) => r.id === option?.value) ?? null)
					}
					search={searchRoles}
					hint={model.resourceServer ? `Resurssipalvelin: ${model.resourceServer.displayName}` : ''}
				/>
			</FormField>

			<FormField class="max-w-lg">
				<FormLabel>Tenant</FormLabel>
				<AutocompleteCombobox
					bind:value={
						() => (model.tenant ? { value: model.tenant.id, label: model.tenant.name } : null),
						(option) => (model.tenant = tenantResults.find((t) => t.id === option?.value) ?? null)
					}
					search={searchTenants}
					disabled={model.tenantDisabled}
				/>
			</FormField>
		</fieldset>

		<div class="modal-action mt-auto">
			<button type="submit" class="btn btn-primary" disabled={!model.canSubmit}>
				{#if model.submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
				Lisää
			</button>
		</div>
	</form>
</Dialog>
