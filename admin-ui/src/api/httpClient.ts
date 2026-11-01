// src/api/httpClient.ts
import type { ErrorDto } from '@/api/generated'
import axios from 'axios'
const http = axios.create({
  baseURL: 'http://localhost:5028/api/admin',
  paramsSerializer: { indexes: null },
})

export class ValidationError extends Error {
  constructor(public readonly errors: ErrorDto[]) {
    super('Validation failed')
  }
}

async function send<T>(method: 'post' | 'put' | 'patch', url: string, body?: unknown): Promise<T> {
  try {
    const { data } = await http.request<T>({ method, url, data: body })
    return data
  } catch (e) {
    if (axios.isAxiosError(e) && Array.isArray(e.response?.data?.errors)) {
      throw new ValidationError(e.response.data.errors)
    }
    throw e
  }
}

export const httpPost = <T>(url: string, body?: unknown) => send<T>('post', url, body)
export const httpPut = <T>(url: string, body?: unknown) => send<T>('put', url, body)
export const httpPatch = <T>(url: string, body?: unknown) => send<T>('patch', url, body)

export async function httpGet<T>(url: string, params?: object): Promise<T> {
  const { data } = await http.get<T>(url, { params })
  return data
}
export async function httpGetOrNull<T>(url: string, params?: object): Promise<T | null> {
  try {
    const { data } = await http.get<T>(url, { params })
    return data || null
  } catch (e) {
    if (axios.isAxiosError(e) && e.response?.status === 404) {
      return null
    }
    throw e
  }
}

export async function httpDelete(url: string): Promise<void> {
  await http.delete(url)
}
