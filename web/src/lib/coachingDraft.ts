const DRAFT_KEY = 'grok-career-coach-draft'

export interface CoachingDraft {
  jobDescription: string
  resumeHighlights: string
}

export function loadCoachingDraft(): CoachingDraft {
  try {
    const raw = sessionStorage.getItem(DRAFT_KEY)
    if (!raw) {
      return { jobDescription: '', resumeHighlights: '' }
    }

    const parsed = JSON.parse(raw) as Partial<CoachingDraft>
    return {
      jobDescription:
        typeof parsed.jobDescription === 'string' ? parsed.jobDescription : '',
      resumeHighlights:
        typeof parsed.resumeHighlights === 'string'
          ? parsed.resumeHighlights
          : '',
    }
  } catch {
    return { jobDescription: '', resumeHighlights: '' }
  }
}

export function saveCoachingDraft(draft: CoachingDraft): void {
  sessionStorage.setItem(DRAFT_KEY, JSON.stringify(draft))
}
