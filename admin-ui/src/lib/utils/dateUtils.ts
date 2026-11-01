const dateFormat = new Intl.DateTimeFormat('fi-FI', { dateStyle: 'short' })
const dateTimeFormat = new Intl.DateTimeFormat('fi-FI', { dateStyle: 'short', timeStyle: 'short' })

export function formatIsoDate(iso: string | null | undefined): string {
  return iso ? dateFormat.format(new Date(iso)) : ''
}

export function formatIsoDateTime(iso: string | null | undefined): string {
  return iso ? dateTimeFormat.format(new Date(iso)) : ''
}
