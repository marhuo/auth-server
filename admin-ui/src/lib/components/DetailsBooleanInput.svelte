<script lang="ts">
	import { ValidationError } from '#lib/api/httpClient.ts';

	// A label and a switch that saves immediately, without a dialog. While saving the switch ignores
	// changes; if saving fails, it returns to the saved value and the error is shown on the row.
	interface Props {
		label: string;
		value: boolean;
		hint?: string;
		editable?: boolean;
		onSave?: (value: boolean) => Promise<void>;
	}

	let { label, value, hint, editable = true, onSave }: Props = $props();

	const id = $props.id();

	let saving = $state(false);
	let errors = $state<string[]>([]);
	// Shown while saving, so the switch moves at once.
	let pending = $state<boolean | null>(null);

	const checked = $derived(pending ?? value);

	async function onChange(e: Event & { currentTarget: HTMLInputElement }) {
		// Not disabled while saving (that would flash the not-allowed cursor and the dimmed style),
		// so a change during a save, e.g. from the keyboard, is undone here.
		if (saving) {
			e.currentTarget.checked = checked;
			return;
		}

		const newValue = e.currentTarget.checked;
		pending = newValue;
		saving = true;
		errors = [];
		try {
			await onSave?.(newValue);
		} catch (err) {
			if (err instanceof ValidationError) {
				errors = err.errors.map((x) => x.messageFi);
				return;
			}
			throw err;
		} finally {
			pending = null;
			saving = false;
		}
	}
</script>

<li class="list-row items-center">
	<div class="list-col-grow">
		<label for={id} class="text-xs opacity-60">{label}</label>
		{#if hint}
			<div class="text-sm opacity-60">{hint}</div>
		{/if}
		{#each errors as msg, index (index)}
			<div class="text-sm text-error">{msg}</div>
		{/each}
	</div>

	<input
		{id}
		type="checkbox"
		class={['toggle toggle-accent', saving && 'pointer-events-none']}
		{checked}
		aria-busy={saving}
		disabled={!editable || !onSave}
		onchange={onChange}
	/>
</li>
