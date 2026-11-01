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
				pages: '../AuthServer/AuthServer.Web/wwwroot/admin',
				assets: '../AuthServer/AuthServer.Web/wwwroot/admin',
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
	proxy: {
			'/api': 'http://localhost:5028',
			'/Logout': 'http://localhost:5028',
			'/Login': 'http://localhost:5028'
		}
	}
});
