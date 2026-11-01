<script>
	import TenantDetailsPage from '#lib/pages/tenants/TenantDetailsPage.svelte';
	import { page } from '$app/state';
</script>

{#if page.params.id}
	{#key page.params.id}
		<TenantDetailsPage />
	{/key}
{/if}
