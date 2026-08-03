const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

let accessToken = ''
let refreshToken = ''

export interface User {
  id: string
  email: string
}

export interface TokenResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: User
}

export interface CoachingRequest {
  jobDescription: string
  resumeHighlights: string
}

export interface CoachingResponse {
  fitSummary: string
  strengths: string[]
  gaps: string[]
  interviewQuestions: string[]
  improvementSuggestions: string[]
}

export interface CoachingSession {
  id: string
  jobDescription: string
  resumeHighlights: string
  result: CoachingResponse
  createdAt: string
}

export async function authenticateWithGoogle(idToken: string): Promise<User> {
  const response = await fetch(`${apiBaseUrl}/api/auth/google`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ idToken }),
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as {
      detail?: string
    } | null
    throw new Error(problem?.detail ?? 'Authentication failed.')
  }

  const tokens = (await response.json()) as TokenResponse
  setTokens(tokens)
  return tokens.user
}

export function signOut() {
  accessToken = ''
  refreshToken = ''
}

export async function analyzeCareerFit(
  request: CoachingRequest,
): Promise<CoachingResponse> {
  const response = await authorizedFetch('/api/coaching/analyze', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(
      response.status === 400
        ? 'Add more detail to both fields before analyzing.'
        : response.status === 401
          ? 'Your session expired. Sign in again.'
        : 'The coaching service is unavailable.',
    )
  }

  return response.json() as Promise<CoachingResponse>
}

export async function listCoachingSessions(): Promise<CoachingSession[]> {
  const response = await authorizedFetch('/api/coaching/sessions', {
    method: 'GET',
  })

  if (!response.ok) {
    throw new Error(
      response.status === 401
        ? 'Your session expired. Sign in again.'
        : 'Unable to load coaching history.',
    )
  }

  return response.json() as Promise<CoachingSession[]>
}

async function authorizedFetch(path: string, init: RequestInit) {
  let response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: withAuthorization(init.headers),
  })

  if (response.status === 401 && refreshToken) {
    const refreshed = await refreshSession()
    if (refreshed) {
      response = await fetch(`${apiBaseUrl}${path}`, {
        ...init,
        headers: withAuthorization(init.headers),
      })
    }
  }

  return response
}

async function refreshSession() {
  const response = await fetch(`${apiBaseUrl}/api/auth/refresh`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ refreshToken }),
  })
  if (!response.ok) {
    signOut()
    return false
  }

  setTokens((await response.json()) as TokenResponse)
  return true
}

function withAuthorization(headers: HeadersInit | undefined) {
  const result = new Headers(headers)
  result.set('Authorization', `Bearer ${accessToken}`)
  return result
}

function setTokens(tokens: TokenResponse) {
  accessToken = tokens.accessToken
  refreshToken = tokens.refreshToken
}
