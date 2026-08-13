import type { CoachingResponse } from '../api/client'

function formatSection(title: string, items: string[]): string {
  if (items.length === 0) {
    return `${title}\n(none)`
  }

  return `${title}\n${items.map((item) => `- ${item}`).join('\n')}`
}

export function formatCoachingBriefText(result: CoachingResponse): string {
  return [
    'Grok Career Coach — Coaching brief',
    '',
    result.fitSummary,
    '',
    formatSection('Strengths', result.strengths),
    '',
    formatSection('Gaps to address', result.gaps),
    '',
    formatSection('Interview questions', result.interviewQuestions),
    '',
    formatSection('Next improvements', result.improvementSuggestions),
  ].join('\n')
}

export async function copyCoachingBrief(
  result: CoachingResponse,
): Promise<void> {
  const text = formatCoachingBriefText(result)

  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(text)
    return
  }

  const textarea = document.createElement('textarea')
  textarea.value = text
  textarea.setAttribute('readonly', '')
  textarea.style.position = 'fixed'
  textarea.style.left = '-9999px'
  document.body.appendChild(textarea)
  textarea.select()
  const copied = document.execCommand('copy')
  document.body.removeChild(textarea)

  if (!copied) {
    throw new Error('Clipboard copy is not available in this browser.')
  }
}
