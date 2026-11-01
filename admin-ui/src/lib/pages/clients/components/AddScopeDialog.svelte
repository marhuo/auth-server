<script lang="ts">
	import type { ClientDtoTypes, ScopeDto } from '#lib/api/generated/index.ts';
	import { ValidationError } from '#lib/api/httpClient.ts';
	import { getScopesSelectableForClient } from '#lib/api/scopeApi.ts';
	import CompactPagination from '#lib/components/CompactPagination.svelte';
	import Dialog from '#lib/components/Dialog.svelte';
	import GeneralErrors from '#lib/components/GeneralErrors.svelte';
	import { InMemoryPagedList } from '#lib/models/PagedList.svelte.ts';
	import Icon from '@iconify/svelte';

	interface Props {
		selected: ScopeDto[];
		resourceServerId: string;
		clientType: ClientDtoTypes;
		onAdd: (scope: ScopeDto) => Promise<void>;
		onClose: () => void;
	}

	let { selected, resourceServerId, clientType, onAdd, onClose }: Props = $props();

	let errors = $state<string[]>([]);

	const list = new InMemoryPagedList<ScopeDto>([], (scope, term) =>
		(scope.name + ' ' + scope.displayName).toLowerCase().includes(term)
	);

	$effect(() => {
		const selectedIds = selected.map((s) => s.id);
		getScopesSelectableForClient(resourceServerId, clientType).then((allScopes) => {
			list.setAll(allScopes.filter((s) => !selectedIds.includes(s.id)));
			errors = [];
		});
	});

	async function onAddClick(scope: ScopeDto) {
		try {
			await onAdd(scope);
			errors = [];
		} catch (e) {
			if (e instanceof ValidationError) {
				errors = e.errors.map((e) => e.messageFi);
				return null;
			}
			throw e;
		}
	}
</script>

<Dialog open={true}>
	<div class="modal-box flex flex-col gap-4">
		<div class="flex items-center justify-between">
			<h3 class="text-lg font-semibold">Lisää scope</h3>
			<button
				type="button"
				class="btn btn-square btn-ghost btn-sm"
				aria-label="Sulje"
				onclick={onClose}
			>
				<Icon icon="ic:baseline-close" width="20" />
			</button>
		</div>

		<label class="input w-full max-w-xs">
			<Icon icon="ic:outline-search" width="18" class="opacity-50" />
			<input
				type="search"
				placeholder="Hae"
				value={list.searchTerm}
				oninput={(e) => list.setSearchTerm(e.currentTarget.value)}
			/>
		</label>

		<GeneralErrors messages={errors} />

		<ul class="list">
			{#each list.items as scope (scope.id)}
				<li class="list-row items-center">
					<div class="list-col-grow">
						<div>{scope.name}</div>
						<div class="text-sm opacity-60">{scope.displayName}</div>
					</div>
					<button type="button" class="btn btn-ghost btn-sm" onclick={() => onAddClick(scope)}>
						<Icon icon="ic:baseline-add" width="18" />
						Lisää
					</button>
				</li>
			{:else}
				<li class="p-4 text-sm opacity-60">Ei lisättäviä scopeja.</li>
			{/each}
		</ul>

		<CompactPagination {list} />
	</div>
</Dialog>
