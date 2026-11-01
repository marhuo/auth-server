<script lang="ts" module>
	export interface SelectOption {
		value: string;
		label: string;
	}
</script>

<script lang="ts">
	import Icon from '@iconify/svelte';
	import { getFieldId } from '#lib/components/FormField.svelte';

	interface Props {
		value: SelectOption | null;
		search: (text: string) => Promise<SelectOption[]>;
		errors?: string[];
		hint?: string;
		required?: boolean;
		disabled?: boolean;
	}

	let {
		value = $bindable(null),
		search,
		errors = [],
		hint,
		required = false,
		disabled = false
	}: Props = $props();

	const ownId = $props.id();
	const id = getFieldId() ?? ownId;

	let text = $derived(value ? value.label : '');
	let options = $state<SelectOption[]>([]);
	let open = $state(false);
	let loading = $state(false);
	let active = $state(-1);
	let searchTimer: ReturnType<typeof setTimeout> | undefined;
	let requestId = 0;

	async function runSearch(term: string) {
		const current = ++requestId;
		loading = true;
		try {
			const result = await search(term);
			if (current === requestId) {
				options = result;
				active = -1;
			}
		} finally {
			if (current === requestId) loading = false;
		}
	}

	function onInput(e: Event & { currentTarget: HTMLInputElement }) {
		text = e.currentTarget.value;
		open = true;
		if (!text) value = null;
		clearTimeout(searchTimer);
		searchTimer = setTimeout(() => runSearch(text), 300);
	}

	function onFocus() {
		open = true;
		runSearch('');
	}

	function onBlur() {
		open = false;
		text = value ? value.label : '';
	}

	function select(option: SelectOption) {
		value = option;
		open = false;
	}

	function clear() {
		value = null;
		options = [];
	}

	function onKeydown(e: KeyboardEvent) {
		if (e.key === 'ArrowDown') {
			e.preventDefault();
			open = true;
			active = Math.min(active + 1, options.length - 1);
		} else if (e.key === 'ArrowUp') {
			e.preventDefault();
			active = Math.max(active - 1, 0);
		} else if (e.key === 'Enter' && open && active >= 0) {
			e.preventDefault();
			select(options[active]);
		} else if (e.key === 'Escape') {
			open = false;
		}
	}
</script>

<div class="relative">
	<label class={['input w-full', { 'input-error': errors.length }]}>
		<input
			{id}
			value={text}
			oninput={onInput}
			onfocus={onFocus}
			onblur={onBlur}
			onkeydown={onKeydown}
			{required}
			{disabled}
			autocomplete="one-time-code"
			role="combobox"
			aria-expanded={open}
			aria-controls="{id}-list"
		/>
		{#if loading}
			<span class="loading loading-xs loading-spinner"></span>
		{/if}
		{#if value && !disabled}
			<button
				type="button"
				class="btn btn-circle btn-ghost btn-xs"
				aria-label="Tyhjennä"
				onclick={clear}
			>
				✕
			</button>
		{/if}
		<Icon
			icon="ic:baseline-keyboard-arrow-down"
			width="20"
			class="shrink-0 opacity-60 transition-transform {open ? 'rotate-180' : ''}"
		/>
	</label>

	{#if open}
		<ul
			id="{id}-list"
			class="menu absolute z-20 w-full rounded-box border border-base-300 bg-base-100 p-1 shadow-sm"
		>
			{#each options as option, i (option.value)}
				<li>
					<button
						type="button"
						class={{ 'menu-active': i === active }}
						onmousedown={(e) => e.preventDefault()}
						onclick={() => select(option)}
					>
						{option.label}
					</button>
				</li>
			{/each}
			{#if !loading && options.length === 0}
				<li class="menu-disabled"><span>Ei tuloksia</span></li>
			{/if}
		</ul>
	{/if}
</div>

{#if hint}
	<p class="label">{hint}</p>
{/if}
{#each errors as msg, index (index)}
	<p class="label text-error">{msg}</p>
{/each}
