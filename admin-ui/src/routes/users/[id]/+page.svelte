<script>
	import UserDetailsPage from '#lib/pages/users/UserDetailsPage.svelte';
	import { page } from '$app/state';
</script>

{#if page.params.id}
	{#key page.params.id}
		<UserDetailsPage />
	{/key}
{/if}
