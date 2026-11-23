import tailwindcss from '@tailwindcss/vite';
import { paraglideVitePlugin } from '@inlang/paraglide-js';
import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [
		tailwindcss(),
		sveltekit({
			compilerOptions: {
				runes: ({ filename }) =>
					filename.split(/[/\\]/).includes('node_modules') ? undefined : true
			},

			adapter: adapter({
				pages: '../backend/AuthServer.Web/wwwroot/admin',
				assets: '../backend/AuthServer.Web/wwwroot/admin',
				fallback: 'index.html'
			}),

			paths: { base: '/admin' }
		}),

		paraglideVitePlugin({
			project: './project.inlang',
			outdir: './src/lib/paraglide',
			emitTsDeclarations: true
		})
	],

	server: {
		port: 5174,
		strictPort: true,
		// Backend addresses: the API and the Razor sign-in pages with their styles and fonts.
		proxy: {
			'/api': 'http://localhost:5028',
			'/Logout': 'http://localhost:5028',
			'/Login': 'http://localhost:5028',
			'/Register': 'http://localhost:5028',
			'/ForgotPassword': 'http://localhost:5028',
			'/SetPassword': 'http://localhost:5028',
			'/PasswordSet': 'http://localhost:5028',
			'/ExternalLogin': 'http://localhost:5028',
			'/DevExternalLogin': 'http://localhost:5028',
			'/css': 'http://localhost:5028',
			'/fonts': 'http://localhost:5028'
		}
	}
});
