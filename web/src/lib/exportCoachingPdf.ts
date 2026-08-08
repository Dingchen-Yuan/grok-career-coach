import type { jsPDF } from 'jspdf'
import type { CoachingResponse } from '../api/client'

const MARGIN = 18
const PAGE_WIDTH = 210
const PAGE_HEIGHT = 297
const CONTENT_WIDTH = PAGE_WIDTH - MARGIN * 2

function addSection(
  doc: jsPDF,
  title: string,
  items: string[],
  y: number,
): number {
  if (y > PAGE_HEIGHT - 40) {
    doc.addPage()
    y = MARGIN
  }

  doc.setFont('helvetica', 'bold')
  doc.setFontSize(11)
  doc.setTextColor(37, 96, 68)
  doc.text(title.toUpperCase(), MARGIN, y)
  y += 7

  doc.setFont('helvetica', 'normal')
  doc.setFontSize(10)
  doc.setTextColor(23, 33, 27)

  for (const item of items) {
    const lines = doc.splitTextToSize(`•  ${item}`, CONTENT_WIDTH) as string[]
    const blockHeight = lines.length * 5 + 3

    if (y + blockHeight > PAGE_HEIGHT - MARGIN) {
      doc.addPage()
      y = MARGIN
    }

    doc.text(lines, MARGIN, y)
    y += blockHeight
  }

  return y + 4
}

export async function downloadCoachingPdf(
  result: CoachingResponse,
  options?: { jobSnippet?: string },
): Promise<void> {
  const { jsPDF } = await import('jspdf')
  const doc = new jsPDF({ unit: 'mm', format: 'a4' })
  const generatedAt = new Date().toLocaleString()

  doc.setFont('helvetica', 'bold')
  doc.setFontSize(16)
  doc.setTextColor(37, 96, 68)
  doc.text('Grok Career Coach', MARGIN, MARGIN + 2)

  doc.setFont('helvetica', 'normal')
  doc.setFontSize(9)
  doc.setTextColor(93, 102, 95)
  doc.text(`Coaching brief · ${generatedAt}`, MARGIN, MARGIN + 9)

  let y = MARGIN + 20

  doc.setDrawColor(201, 200, 188)
  doc.line(MARGIN, y, PAGE_WIDTH - MARGIN, y)
  y += 10

  doc.setFont('helvetica', 'bold')
  doc.setFontSize(13)
  doc.setTextColor(23, 33, 27)
  const summaryLines = doc.splitTextToSize(
    result.fitSummary,
    CONTENT_WIDTH,
  ) as string[]
  doc.text(summaryLines, MARGIN, y)
  y += summaryLines.length * 6 + 8

  if (options?.jobSnippet?.trim()) {
    doc.setFont('helvetica', 'italic')
    doc.setFontSize(9)
    doc.setTextColor(93, 102, 95)
    const snippet = doc.splitTextToSize(
      `Role context: ${options.jobSnippet.trim().slice(0, 280)}${
        options.jobSnippet.trim().length > 280 ? '…' : ''
      }`,
      CONTENT_WIDTH,
    ) as string[]
    doc.text(snippet, MARGIN, y)
    y += snippet.length * 4.5 + 8
  }

  y = addSection(doc, 'Strengths', result.strengths, y)
  y = addSection(doc, 'Gaps to address', result.gaps, y)
  y = addSection(doc, 'Interview questions', result.interviewQuestions, y)
  addSection(doc, 'Next improvements', result.improvementSuggestions, y)

  const stamp = new Date().toISOString().slice(0, 10)
  doc.save(`grok-career-coach-brief-${stamp}.pdf`)
}
