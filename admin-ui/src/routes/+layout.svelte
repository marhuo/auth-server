<script lang="ts">
	import './layout.css';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import Icon from '@iconify/svelte';
	import favicon from '#lib/assets/favicon.ico';
	import logo from '#lib/assets/logo-32x32.png';
	let { children } = $props();
	let sidebarOpen = $state(false);

	const navItems = [
		{ title: 'Tenantit', href: resolve('/tenants'), icon: 'ic:outline-domain' },
		{ title: 'Clientit', href: resolve('/clients'), icon: 'ic:outline-apps' },
		{ title: 'Resurssipalvelimet', href: resolve('/resource-servers'), icon: 'ic:outline-dns' },
		{ title: 'Käyttäjät', href: resolve('/users'), icon: 'ic:outline-group' },
		{ title: 'Scopet', href: resolve('/scopes'), icon: 'ic:outline-key' },
		{ title: 'Roolit', href: resolve('/roles'), icon: 'ic:outline-admin-panel-settings' }
	];

	const homeHref = resolve('/');

	const isActive = (href: string) => page.url.pathname.startsWith(href);
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>
<div class="drawer md:drawer-open">
	<input id="sidebar" type="checkbox" class="drawer-toggle" bind:checked={sidebarOpen} />

	<div class="drawer-content flex min-h-screen flex-col">
		<header class="navbar min-h-12">
			<label for="sidebar" class="btn btn-square btn-ghost md:hidden" aria-label="Valikko">
				<Icon icon="ic:baseline-menu" width="24" />
			</label>
			<a href={homeHref} class="ml-4 flex items-center gap-2 md:hidden">
				<img src={logo} alt="" height="28" />
				<span class="font-semibold">Auth server</span>
			</a>
			<div class="flex-1"></div>
			<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
			<a href="/Logout" data-sveltekit-reload class="btn btn-ghost">
				<Icon icon="ic:outline-logout" width="16" />
				Kirjaudu ulos
			</a>
		</header>

		<main class="flex-1 pt-0 pr-6 pb-6 pl-6">
			<div class="max-w-5xl">
				{@render children()}
			</div>
		</main>
	</div>

	<div class="drawer-side">
		<label for="sidebar" class="drawer-overlay" aria-label="Sulje valikko"></label>
		<div class="flex min-h-full w-60 flex-col bg-base-100">
			<a href={resolve('/')} class="flex h-16 items-center gap-2 px-4">
				<img src={logo} alt="" height="28" />
				<span class="font-semibold">Auth server</span>
			</a>

			<ul class="menu w-full gap-2 menu-md p-2">
				{#each navItems as item (item.href)}
					<li>
						<a
							href={item.href}
							class={{ 'menu-active': isActive(item.href) }}
							onclick={() => (sidebarOpen = false)}
						>
							<Icon icon={item.icon} width="20" />
							{item.title}
						</a>
					</li>
				{/each}
			</ul>
		</div>
	</div>
</div>
