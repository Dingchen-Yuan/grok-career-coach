const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

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

export async function analyzeCareerFit(
  request: CoachingRequest,
): Promise<CoachingResponse> {
  const response = await fetch(`${apiBaseUrl}/api/coaching/analyze`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(
      response.status === 400
        ? 'Add more detail to both fields before analyzing.'
        : 'The coaching service is unavailable.',
    )
  }

  return response.json() as Promise<CoachingResponse>
}
