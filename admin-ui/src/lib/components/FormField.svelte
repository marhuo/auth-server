<script lang="ts" module>
	import { getContext, setContext } from 'svelte';
	const KEY = Symbol('form-field');
	export function getFieldId(): string | undefined {
		return getContext<string>(KEY);
	}
</script>

<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		class?: string;
		children: Snippet;
	}

	let { class: className = '', children }: Props = $props();

	const id = $props.id();
	setContext(KEY, id);
</script>

<div class={['flex flex-col gap-1', className]}>
	{@render children()}
</div>
