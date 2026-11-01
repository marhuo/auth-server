<script>
	import { page } from '$app/state';
	import ScopeDetailsPage from '#lib/pages/scopes/ScopeDetailsPage.svelte';
</script>

{#if page.params.id}
	{#key page.params.id}
		<ScopeDetailsPage />
	{/key}
{/if}
