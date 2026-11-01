// src/lib/api/httpClient.ts
import type { ErrorDto } from './generated';
const PUBLIC_API_URL = '/api/admin';
export type Fetch = typeof fetch;

export class ValidationError extends Error {
	readonly errors: ErrorDto[];
	constructor(errors: ErrorDto[]) {
		super('Validation failed');
		this.errors = errors;
	}
}

export class HttpError extends Error {
	readonly status: number;
	readonly body: unknown;
	constructor(status: number, body: unknown) {
		super(`HTTP ${status}`);
		this.status = status;
		this.body = body;
	}
}

// Vastaa axiosin asetusta paramsSerializer: { indexes: null } → ids=a&ids=b
function buildUrl(url: string, params?: object): string {
	const search = new URLSearchParams();
	for (const [key, value] of Object.entries(params ?? {})) {
		if (value == null) continue;
		if (Array.isArray(value)) value.forEach((v) => search.append(key, String(v)));
		else search.append(key, String(value));
	}
	const qs = search.toString();
	return `${PUBLIC_API_URL}${url}${qs ? `?${qs}` : ''}`;
}

async function parseBody(res: Response): Promise<unknown> {
	const text = await res.text();
	if (!text) return undefined;
	const isJson = res.headers.get('content-type')?.includes('json');
	return isJson ? JSON.parse(text) : text;
}

async function request<T>(
	f: Fetch,
	method: string,
	url: string,
	opts: { params?: object; body?: unknown } = {}
): Promise<T> {
	const hasBody = opts.body !== undefined;
	const res = await f(buildUrl(url, opts.params), {
		method,
		headers: hasBody ? { 'Content-Type': 'application/json' } : undefined,
		body: hasBody ? JSON.stringify(opts.body) : undefined
	});
	const data = await parseBody(res);
	if (res.status === 401) {
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		window.location.href = '/Login';
		return new Promise<never>(() => {});
	}
	if (!res.ok) {
		const errors = (data as { errors?: unknown } | undefined)?.errors;
		if (Array.isArray(errors)) throw new ValidationError(errors as ErrorDto[]);
		throw new HttpError(res.status, data);
	}
	return data as T;
}

export const httpPost = <T>(url: string, body?: unknown, f: Fetch = fetch) =>
	request<T>(f, 'POST', url, { body });
export const httpPut = <T>(url: string, body?: unknown, f: Fetch = fetch) =>
	request<T>(f, 'PUT', url, { body });
export const httpPatch = <T>(url: string, body?: unknown, f: Fetch = fetch) =>
	request<T>(f, 'PATCH', url, { body });

export const httpGet = <T>(url: string, params?: object, f: Fetch = fetch) =>
	request<T>(f, 'GET', url, { params });

export async function httpGetOrNull<T>(url: string, params?: object, f: Fetch = fetch) {
	try {
		return (await httpGet<T>(url, params, f)) || null;
	} catch (e) {
		if (e instanceof HttpError && e.status === 404) return null;
		throw e;
	}
}

export const httpDelete = <T = void>(url: string, f: Fetch = fetch) => request<T>(f, 'DELETE', url);
