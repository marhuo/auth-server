<script lang="ts">
	import type { ResourceServerDto } from '#lib/api/generated/index.ts';
	import { ValidationError } from '#lib/api/httpClient.ts';
	import { getResourceServerPagedList } from '#lib/api/resourceServerApi.ts';
	import AutocompleteCombobox, {
		type SelectOption
	} from '#lib/components/AutocompleteCombobox.svelte';
	import Dialog from '#lib/components/Dialog.svelte';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';

	interface Props {
		label?: string;
		resourceServerId: string | null;
		resourceServerName: string | null;
		hint?: string;
		dialogTitle?: string;
		required?: boolean;
		editable?: boolean;
		onSave?: (resourceServer: ResourceServerDto | null) => Promise<void>;
	}

	let {
		label = 'Resurssipalvelin',
		resourceServerId,
		resourceServerName,
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
	let searchResults: ResourceServerDto[] = [];

	const canSave = $derived(
		!submitting &&
			(selected?.value ?? null) !== resourceServerId &&
			(!required || selected !== null)
	);

	function openDialog() {
		selected = resourceServerId
			? { value: resourceServerId, label: resourceServerName ?? '' }
			: null;
		errors = [];
		editMode = true;
	}

	function closeDialog() {
		editMode = false;
	}

	async function searchServers(text: string): Promise<SelectOption[]> {
		const result = await getResourceServerPagedList({
			pageNumber: 1,
			pageSize: 10,
			searchTerm: text || null
		});
		searchResults = result.items.filter((rs) => !rs.isAuthServer);
		return searchResults.map((rs) => ({ value: rs.id, label: rs.displayName }));
	}

	async function onSubmit(e: SubmitEvent) {
		e.preventDefault();
		submitting = true;
		errors = [];
		try {
			const resourceServer = selected
				? (searchResults.find((rs) => rs.id === selected!.value) ?? null)
				: null;
			await onSave?.(resourceServer);
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
		{#if resourceServerId}
			<a href={resolve('/resource-servers/[id]', { id: resourceServerId })} class="link">
				{resourceServerName}
			</a>
		{:else}
			<div class="opacity-60">Ei resurssipalvelinta</div>
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
						search={searchServers}
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
