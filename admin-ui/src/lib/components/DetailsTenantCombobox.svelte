<script lang="ts">
	import type { TenantDto } from '#lib/api/generated/index.ts';
	import { ValidationError } from '#lib/api/httpClient.ts';
	import { getTenantPagedList } from '#lib/api/tenantApi.ts';
	import AutocompleteCombobox, {
		type SelectOption
	} from '#lib/components/AutocompleteCombobox.svelte';
	import Dialog from '#lib/components/Dialog.svelte';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';

	interface Props {
		label?: string;
		tenantId: string | null;
		tenantName: string | null;
		hint?: string;
		dialogTitle?: string;
		required?: boolean;
		editable?: boolean;
		onSave?: (tenant: TenantDto | null) => Promise<void>;
	}

	let {
		label = 'Tenant',
		tenantId,
		tenantName,
		hint,
		dialogTitle = label,
		required = false,
		editable = true,
		onSave
	}: Props = $props();

	let editMode = $state(false);
	let selected = $state<SelectOption | null>(null);
	let errors = $state<string[]>([]);
	let submitting = $state(false);
	let searchResults: TenantDto[] = [];

	const canSave = $derived(
		!submitting && (selected?.value ?? null) !== tenantId && (!required || selected !== null)
	);

	function openDialog() {
		selected = tenantId ? { value: tenantId, label: tenantName ?? '' } : null;
		errors = [];
		editMode = true;
	}

	function closeDialog() {
		editMode = false;
	}

	async function searchTenants(text: string): Promise<SelectOption[]> {
		const result = await getTenantPagedList({
			pageNumber: 1,
			pageSize: 10,
			searchTerm: text || null
		});
		searchResults = result.items;
		return searchResults.map((t) => ({ value: t.id, label: t.name }));
	}

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		submitting = true;
		errors = [];
		try {
			const tenant = selected
				? (searchResults.find((t) => t.id === selected!.value) ?? null)
				: null;
			await onSave?.(tenant);
			editMode = false;
		} catch (err) {
			if (err instanceof ValidationError) {
				errors = err.errors.map((x) => x.messageFi);
				return;
			}
			throw err;
		} finally {
			submitting = false;
		}
	}
</script>

<li class="list-row items-center">
	<div class="list-col-grow">
		<div class="text-xs opacity-60">{label}</div>
		{#if tenantId}
			<a href={resolve('/tenants/[id]', { id: tenantId })} class="link">{tenantName}</a>
		{:else}
			<div class="opacity-60">Ei tenanttia</div>
		{/if}
	</div>

	{#if editable && onSave}
		<button
			type="button"
			class="btn btn-square btn-ghost btn-sm"
			aria-label="Muokkaa {label}"
			onclick={openDialog}
		>
			<Icon icon="ic:outline-edit" width="18" />
		</button>
	{/if}

	{#if editMode}
		<Dialog open={true}>
			<form class="modal-box flex min-h-56 flex-col gap-4 overflow-visible" onsubmit={onSubmit}>
				<div class="flex items-center justify-between">
					<h3 class="text-lg font-semibold">{dialogTitle}</h3>
					<button
						type="button"
						class="btn btn-square btn-ghost btn-sm"
						aria-label="Sulje"
						disabled={submitting}
						onclick={closeDialog}
					>
						<Icon icon="ic:baseline-close" width="20" />
					</button>
				</div>

				<fieldset class="fieldset" disabled={submitting}>
					<AutocompleteCombobox
						bind:value={selected}
						search={searchTenants}
						{hint}
						{errors}
						{required}
					/>
				</fieldset>

				<div class="modal-action mt-auto">
					<button type="submit" class="btn btn-primary" disabled={!canSave}>
						{#if submitting}<span class="loading loading-sm loading-spinner"></span>{/if}
						Tallenna
					</button>
				</div>
			</form>
		</Dialog>
	{/if}
</li>
