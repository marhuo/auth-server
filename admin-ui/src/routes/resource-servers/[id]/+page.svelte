<script>
	import ResourceServerDetailsPage from '#lib/pages/resource-servers/ResourceServerDetailsPage.svelte';
	import { page } from '$app/state';
</script>

{#if page.params.id}
	{#key page.params.id}
		<ResourceServerDetailsPage />
	{/key}
{/if}
