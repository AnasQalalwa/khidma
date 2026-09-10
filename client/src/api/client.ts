export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
  [key: string]: unknown
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null
  readonly validationErrors: Record<string, string[]>

  constructor(
    message: string,
    status: number,
    problem: ProblemDetails | null = null,
  ) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
    this.validationErrors = problem?.errors ?? {}
  }
}

function readCookie(name: string): string | null {
  const encodedName = `${encodeURIComponent(name)}=`
  const parts = document.cookie.split(';')

  for (const part of parts) {
    const cookie = part.trim()
    if (cookie.startsWith(encodedName) || cookie.startsWith(`${name}=`)) {
      const value = cookie.slice(cookie.indexOf('=') + 1)
      return decodeURIComponent(value)
    }
  }

  return null
}

async function ensureAntiforgeryToken(): Promise<string> {
  const response = await fetch('/api/antiforgery/token', {
    method: 'GET',
    credentials: 'include',
  })

  if (!response.ok) {
    throw new ApiError('Could not obtain an anti-forgery token.', response.status)
  }

  const token = readCookie('XSRF-TOKEN')
  if (!token) {
    throw new ApiError('Anti-forgery token cookie was not set.', 400)
  }

  return token
}

function isUnsafeMethod(method: string): boolean {
  return ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method.toUpperCase())
}

async function parseBody(response: Response): Promise<unknown> {
  if (response.status === 204) {
    return undefined
  }

  const text = await response.text()
  if (!text) {
    return undefined
  }

  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('application/json') || contentType.includes('application/problem+json')) {
    return JSON.parse(text) as unknown
  }

  return text
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'status' in value || 'errors' in value)
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const method = (options.method ?? 'GET').toUpperCase()
  const headers = new Headers(options.headers)

  if (isUnsafeMethod(method)) {
    const token = await ensureAntiforgeryToken()
    headers.set('X-XSRF-TOKEN', token)
  }

  if (options.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  let response: Response
  try {
    response = await fetch(path, {
      ...options,
      method,
      headers,
      credentials: 'include',
    })
  } catch {
    throw new ApiError('Network error. Confirm the API is running.', 0)
  }

  const body = await parseBody(response)

  if (!response.ok) {
    const problem = isProblemDetails(body) ? body : null
    const message =
      problem?.detail ??
      problem?.title ??
      `Request failed with status ${response.status}`
    throw new ApiError(message, response.status, problem)
  }

  return body as T
}

export function fieldError(
  errors: Record<string, string[]> | undefined,
  field: string,
): string | undefined {
  if (!errors) {
    return undefined
  }

  const pascal = field.length === 0 ? field : field[0].toUpperCase() + field.slice(1)
  return errors[field]?.[0] ?? errors[pascal]?.[0]
}

export async function warmupAntiforgery(): Promise<void> {
  try {
    await ensureAntiforgeryToken()
  } catch {
    // Auth bootstrap still proceeds; mutations will retry.
  }
}
