import { useState } from 'react'
import type { FormEvent } from 'react'
import { analyzeCareerFit } from './api/client'
import type { CoachingResponse } from './api/client'
import './App.css'

function App() {
  const [jobDescription, setJobDescription] = useState('')
  const [resumeHighlights, setResumeHighlights] = useState('')
  const [result, setResult] = useState<CoachingResponse | null>(null)
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsLoading(true)

    try {
      setResult(await analyzeCareerFit({ jobDescription, resumeHighlights }))
    } catch (requestError) {
      setError(
        requestError instanceof Error
          ? requestError.message
          : 'Unable to analyze this role right now.',
      )
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <main>
      <header>
        <span className="eyebrow">AI career coach</span>
        <h1>Turn a job description into your next best move.</h1>
        <p>
          Compare a role with your experience and get focused interview
          preparation, gap analysis, and practical résumé improvements.
        </p>
      </header>

      <section className="workspace">
        <form onSubmit={handleSubmit}>
          <label htmlFor="job-description">Job description</label>
          <textarea
            id="job-description"
            minLength={30}
            onChange={(event) => setJobDescription(event.target.value)}
            placeholder="Paste the role, responsibilities, and requirements..."
            required
            value={jobDescription}
          />

          <label htmlFor="resume-highlights">Résumé highlights</label>
          <textarea
            id="resume-highlights"
            minLength={20}
            onChange={(event) => setResumeHighlights(event.target.value)}
            placeholder="Share your experience, skills, and measurable wins..."
            required
            value={resumeHighlights}
          />

          <button disabled={isLoading} type="submit">
            {isLoading ? 'Analyzing…' : 'Analyze my fit'}
          </button>
          {error && <p className="error">{error}</p>}
        </form>

        <article className="results" aria-live="polite">
          {result ? (
            <>
              <span className="eyebrow">Coaching brief</span>
              <h2>{result.fitSummary}</h2>
              <ResultList title="Strengths" items={result.strengths} />
              <ResultList title="Gaps to address" items={result.gaps} />
              <ResultList
                title="Interview questions"
                items={result.interviewQuestions}
              />
              <ResultList
                title="Next improvements"
                items={result.improvementSuggestions}
              />
            </>
          ) : (
            <div className="empty-state">
              <span>01</span>
              <h2>Your coaching brief will appear here.</h2>
              <p>The current API uses a mock Grok client for local development.</p>
            </div>
          )}
        </article>
      </section>
    </main>
  )
}

function ResultList({ title, items }: { title: string; items: string[] }) {
  return (
    <section className="result-group">
      <h3>{title}</h3>
      <ul>
        {items.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </section>
  )
}

export default App
