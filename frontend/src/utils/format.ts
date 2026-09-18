const dateFormatter = new Intl.DateTimeFormat('en-PH', {
  year: 'numeric',
  month: 'long',
  day: 'numeric',
})

export function formatDate(value: string | null): string {
  if (!value) {
    return 'Not published'
  }

  const parsed = new Date(value)

  return Number.isNaN(parsed.getTime()) ? 'Unknown date' : dateFormatter.format(parsed)
}
