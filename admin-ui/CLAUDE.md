# CLAUDE.md — Admin UI

Admin UI for AuthServer: manages tenants, clients, resource servers, users, scopes and roles.

## Overview

SvelteKit 3 + Svelte 5 (runes forced on for all project files), TypeScript (strict), Tailwind CSS 4 + daisyUI 5, Iconify icons. Client-side only SPA (`ssr = false`, `prerender = false` in `src/routes/+layout.ts`), built with `adapter-static` and served by the backend under `/admin`. There are no tests yet.

## Commands

Run from `admin-ui/`:

```bash
npm run dev      # Vite dev server on http://localhost:5174/admin (strict port)
npm run build    # Writes to ../backend/AuthServer.Web/wwwroot/admin (gitignored)
npm run check    # svelte-check (type check)
npm run lint     # prettier --check + eslint
npm run format   # prettier --write
```

After changes, run `npm run check` and `npm run lint`.

The dev server proxies `/api`, the backend's Razor sign-in pages (`/Login`, `/Logout`, `/ExternalLogin`, `/DevExternalLogin`) and their `/css` and `/fonts` to the backend at `http://localhost:5028`, so the backend must be running. Authentication is cookie-based and handled by the backend: a 401 from the API redirects to `/Login`.

## Architecture

```
src/
  routes/<aggregates>/           SvelteKit routing only: each +page.svelte just renders a page from lib/pages
    +page.svelte                 List      (/clients)          → ClientListPage
    create/+page.svelte          Create    (/clients/create)   → ClientCreatePage
    [id]/+page.svelte            Details   (/clients/[id])     → ClientDetailsPage
  lib/
    api/
      httpClient.ts              fetch wrapper; base URL /api/admin; ValidationError, HttpError
      <aggregate>Api.ts          One function per backend endpoint (singular, e.g. clientApi.ts)
      generated/                 TypeGen output from the backend — DO NOT EDIT
    models/                      Shared models, see "Shared models" below
      FormModelBase.svelte.ts    Base class for create forms (submitting state, validation errors)
      PagedList.svelte.ts        PagedListBase / PagedList / InMemoryPagedList
      clientTypes.ts             Client type labels, descriptions and rules
    pages/<aggregates>/          All pages and their view models, one folder per backend aggregate
      components/                Dialogs and widgets used only by this aggregate's pages
    components/                  Shared UI components (inputs, dialogs, comboboxes, pagination)
    utils/                       Generic helpers (collections, dates, normalizers)
```

### Pages and routes

- **All pages live in `lib/pages/<aggregates>/`**, one folder per backend aggregate. The folder name is always plural and kebab-case: `users`, `clients`, `resource-servers`. Current folders: `tenants`, `clients`, `resource-servers`, `users`, `scopes`, `roles`.
- **Page naming:** `<Aggregate>ListPage.svelte`, `<Aggregate>CreatePage.svelte` and `<Aggregate>DetailsPage.svelte`, each with its view model next to it (`<Aggregate>CreateModel.svelte.ts`, `<Aggregate>DetailsModel.svelte.ts`, and `<Aggregate>ListModel.svelte.ts` when the list needs one).
- **`routes/` follows SvelteKit conventions and contains no page logic or markup.** Its folder names match the `lib/pages` folders, and each `+page.svelte` only imports and renders the matching page:

  ```svelte
  <script lang="ts">
  	import ClientListPage from '#lib/pages/clients/ClientListPage.svelte';
  </script>

  <ClientListPage />
  ```

  Pages read route parameters themselves (`page.params.id` from `$app/state`).

- **Adding a new aggregate:** copy the structure of an existing one (e.g. `clients`) into both `lib/pages/` and `routes/`, and add a nav item in `src/routes/+layout.svelte`.

### Generated API types

`src/lib/api/generated/` is produced by TypeGen from the backend (`backend/AuthServer.Web/tgconfig.json`, spec `AdminUiGenerationSpec`). The output directory is cleared on every run, so manual edits are lost. If a DTO or request type is missing or wrong, change the C# type / generation spec in the backend, or ask. TypeGen runs automatically after every backend build (`RunTypeGen` target in `AuthServer.Web.csproj`), so building the backend regenerates the types. TypeGen writes CRLF line endings; `.gitattributes` keeps this folder as CRLF and `.prettierignore` excludes it.

### API layer

- Each `<aggregate>Api.ts` exports small arrow functions wrapping `httpGet` / `httpPost` / `httpPut` / `httpDelete`. URLs are relative to `/api/admin`.
- Use `httpGetOrNull` for fetch-by-id: it returns `null` on 404.
- Batch lookups follow the pattern `getXxxByIds(ids)` (returns `[]` for empty input without a request) and `getXxxMapByIds(ids)` (dedupes and returns a `Map`). Use them to resolve related names in lists instead of fetching per row.
- Backend validation errors (response body with `errors: ErrorDto[]`) are thrown as `ValidationError`. Each `ErrorDto` has `key` (field name), `messageFi` and `messageEn`. The UI shows `messageFi`.

### Shared models (`lib/models/`)

`lib/models/` holds models shared across pages. There is no separate domain folder, because the frontend domain logic is thin. It contains:

- **View model base classes:** `FormModelBase`, `PagedListBase` and related classes.
- **Domain concepts:** labels, descriptions and rules for backend enums and types (e.g. `clientTypes.ts`). Use a `Record<EnumType, Info>` so that a new backend value breaks the build until it is handled. Pages and view models call these helpers instead of comparing enum values directly.
- **DTO enrichment:** shared logic that turns DTOs into UI-friendly shapes (resolved names, labels), when more than one page needs it.

Code used by a single aggregate's pages stays in `pages/<aggregates>/`. Generic helpers that know nothing about the domain go in `utils/`.

### View models (`*.svelte.ts`)

Page state and logic live in classes in `.svelte.ts` files next to the page; the `.svelte` component stays mostly markup.

- **Reactive state:** `private _x = $state(...)` with a public getter. Use a setter only when assignment has side effects (normalization, reloading dependent data). Plain `x = $state(...)` public fields are fine for simple form inputs bound with `bind:value`.
- **Methods are arrow-function properties** (`load = async () => {...}`) so they can be passed directly as callbacks (`onAdd={client.addRedirectUri}`).
- **List models:** extend `PagedListBase<TItem>` and implement `fetch()` when items need mapping/enrichment (see `ClientListModel`); otherwise use `new PagedList(getXxxPagedList)`. Use `InMemoryPagedList` for client-side paging of an already loaded array. Render pagination with `CompactPagination`.
- **Create models:** extend `FormModelBase` and implement `submit = () => this.run(request, createXxx)`. `run` handles the submitting flag and `ValidationError`s; bind field errors with `model.errorsFor('fieldName')` and show the rest with `<GeneralErrors messages={model.generalErrors} />`. On success the page navigates to the details page with `goto(resolve('/xxx/[id]', { id }), { replace: true })`.
- **Details models:** take the id in the constructor, expose `loading` / `notFound`, and have one method per edit action that calls the API and then updates local state (no full reload).
- **Async races:** when a reload depends on changing inputs, capture the inputs before `await` and discard the result if they changed (see `ClientCreateModel.reloadScopes`, `PagedListBase.load`).

### Components and UI patterns

- **Where components go:**
  - `lib/components/` holds general-purpose components that pages of any aggregate can use: inputs, dialogs, pagination and comboboxes such as `TenantCombobox`.
  - `lib/pages/<aggregates>/components/` holds components meant only for that aggregate's pages, e.g. `pages/clients/components/AddUriDialog.svelte`. Pages of other aggregates must not import them.
  - Put a new component in the aggregate's `components/` folder by default. Move it to `lib/components/` only when another aggregate needs it, and make it general at the same time (no assumptions about the original aggregate in props or texts).
- Props via `interface Props` + `let {...}: Props = $props()`; two-way props use `$bindable()`. Use snippets / `{@render}`, not slots. Use `$derived` / `$derived.by` for computed values.
- Forms: `<FormField>` + `<FormLabel>` + an input component (`TextInput`, `PasswordInput`, `TextAreaInput`, `RadioGroup`, comboboxes…). `FormField` provides the input id via context. Inputs accept `errors`, `hint`, `required`.
- Details pages: daisyUI `card` sections with `list` rows. Editable fields use `Details*` components (`DetailsTextInput`, `DetailsTenantCombobox`, …), which open an edit dialog and call an `onSave` that returns a promise; they show `ValidationError`s inside the dialog.
- Dialogs: wrap content in `<Dialog open={true}>` and mount the dialog conditionally (`{#if xxxDialogOpen}`), so its state resets each time. Deletion uses `DeleteDialog` / `DeleteIconButton`.
- Destructive or extra actions on a details page go in `ActionsMenu`.

### Styling

- Use daisyUI component classes and Tailwind utilities. Avoid `<style>` blocks and hard-coded colors; use theme tokens (`base-100/200/300`, `primary`, `accent`, `error`, …).
- Custom themes `auth-light` (default) and `auth-dark` (prefers-dark) are defined in `src/routes/layout.css`. Global style tweaks go there too.
- Icons: `<Icon icon="ic:..." />` from `@iconify/svelte`, using the Google Material `ic:` set.

## Rules

1. **Imports:** use the `#lib/...` alias (defined in `package.json` `imports`), not relative paths or `$lib`, and include the file extension (e.g. `#lib/api/clientApi.ts`).
2. **Internal links and navigation** must go through `resolve()` from `$app/paths` (the app runs under base path `/admin`). ESLint enforces this (`svelte/no-navigation-without-resolve`). Links to backend pages outside the SPA (`/Login`, `/Logout`, `/hangfire`, …) are plain URLs with an `eslint-disable-next-line` comment.
3. **Language:** user-facing UI text is Finnish and is written directly in the components. Paraglide is installed (`messages/`, `project.inlang/`) but not used yet — do not start using it unless asked. Code, identifiers and comments are in English.
4. **Formatting:** Prettier — tabs, single quotes, no trailing commas, print width 100, Tailwind class sorting. Run `npm run format` rather than formatting by hand.
5. **Keyed `#each`:** always give `{#each}` blocks a key (`(item.id)`).
6. **No new dependencies** without asking.
