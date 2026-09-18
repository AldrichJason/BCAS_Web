const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5216'

/** An error carrying the status and the ProblemDetails message sent by the API. */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

type RefreshHandler = () => Promise<string | null>

let accessToken: string | null = null
let refreshHandler: RefreshHandler | null = null

export function setAccessToken(token: string | null): void {
  accessToken = token
}

/**
 * Registered by the auth provider so an expired access token is renewed once,
 * transparently, before the request is retried.
 */
export function setRefreshHandler(handler: RefreshHandler | null): void {
  refreshHandler = handler
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  signal?: AbortSignal
  /** Set for endpoints that must not trigger the refresh-and-retry dance. */
  skipAuthRetry?: boolean
}

async function readError(response: Response): Promise<string> {
  try {
    const problem = (await response.json()) as { detail?: string; title?: string; errors?: Record<string, string[]> }

    if (problem.errors) {
      const messages = Object.values(problem.errors).flat()
      if (messages.length > 0) {
        return messages.join(' ')
      }
    }

    return problem.detail ?? problem.title ?? response.statusText
  } catch {
    return response.statusText || 'The request failed.'
  }
}

async function send(path: string, options: RequestOptions): Promise<Response> {
  const headers: Record<string, string> = { Accept: 'application/json' }

  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`
  }

  return fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
  })
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let response = await send(path, options)

  if (response.status === 401 && !options.skipAuthRetry && refreshHandler) {
    const renewed = await refreshHandler()

    if (renewed) {
      response = await send(path, options)
    }
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readError(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export function toQueryString(params: Record<string, string | number | boolean | undefined | null>): string {
  const search = new URLSearchParams()

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value))
    }
  }

  const query = search.toString()

  return query ? `?${query}` : ''
}
