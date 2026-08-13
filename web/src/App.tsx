import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import {
  analyzeCareerFit,
  authenticateWithGoogle,
  deleteCoachingSession,
  listCoachingSessions,
  signOut,
} from './api/client'
import type { CoachingResponse, CoachingSession, User } from './api/client'
import { downloadCoachingPdf } from './lib/exportCoachingPdf'
import { copyCoachingBrief } from './lib/copyCoachingBrief'
import './App.css'

function App() {
  const [user, setUser] = useState<User | null>(null)
  const [authError, setAuthError] = useState('')
  const [isAuthenticating, setIsAuthenticating] = useState(false)
  const [jobDescription, setJobDescription] = useState('')
  const [resumeHighlights, setResumeHighlights] = useState('')
  const [result, setResult] = useState<CoachingResponse | null>(null)
  const [selectedSessionId, setSelectedSessionId] = useState<string | null>(null)
  const [sessions, setSessions] = useState<CoachingSession[]>([])
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [historyError, setHistoryError] = useState('')
  const [deletingSessionId, setDeletingSessionId] = useState<string | null>(null)
  const [copyStatus, setCopyStatus] = useState<'idle' | 'copied' | 'error'>(
    'idle',
  )

  async function loadSessions() {
    setSessions(await listCoachingSessions())
  }

  async function handleGoogleCredential(idToken: string) {
    setAuthError('')
    setIsAuthenticating(true)

    try {
      setUser(await authenticateWithGoogle(idToken))
      await loadSessions()
    } catch (requestError) {
      setAuthError(
        requestError instanceof Error
          ? requestError.message
          : 'Unable to authenticate right now.',
      )
    } finally {
      setIsAuthenticating(false)
    }
  }

  function handleSignOut() {
    signOut()
    setUser(null)
    setResult(null)
    setSelectedSessionId(null)
    setSessions([])
    setHistoryError('')
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsLoading(true)

    try {
      setResult(await analyzeCareerFit({ jobDescription, resumeHighlights }))
      setSelectedSessionId(null)
      await loadSessions()
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

  async function handleDeleteSession(sessionId: string) {
    setHistoryError('')
    setDeletingSessionId(sessionId)

    try {
      await deleteCoachingSession(sessionId)
      if (selectedSessionId === sessionId) {
        setResult(null)
        setSelectedSessionId(null)
      }
      await loadSessions()
    } catch (requestError) {
      setHistoryError(
        requestError instanceof Error
          ? requestError.message
          : 'Unable to delete this coaching session.',
      )
    } finally {
      setDeletingSessionId(null)
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
        {user && (
          <div className="account">
            <span>Signed in as {user.email}</span>
            <button className="text-button" onClick={handleSignOut} type="button">
              Sign out
            </button>
          </div>
        )}
      </header>

      {!user ? (
        <section className="auth-panel">
          <div>
            <span className="eyebrow">Secure Google sign-in</span>
            <h2>Save every coaching session securely.</h2>
            <p>
              Sign in with your Google account. Google verifies your identity;
              this app stores your coaching history under your verified email.
            </p>
          </div>
          <div className="auth-form">
            <GoogleSignInButton
              disabled={isAuthenticating}
              onCredential={handleGoogleCredential}
              onError={setAuthError}
            />
            {isAuthenticating && <p>Verifying your Google account…</p>}
            {authError && <p className="error">{authError}</p>}
          </div>
        </section>
      ) : (
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
                <div className="results-header">
                  <span className="eyebrow">Coaching brief</span>
                  <div className="results-actions">
                    <button
                      className="secondary-button"
                      onClick={() => {
                        void copyCoachingBrief(result)
                          .then(() => {
                            setCopyStatus('copied')
                            window.setTimeout(
                              () => setCopyStatus('idle'),
                              2000,
                            )
                          })
                          .catch(() => {
                            setCopyStatus('error')
                            window.setTimeout(
                              () => setCopyStatus('idle'),
                              2500,
                            )
                          })
                      }}
                      type="button"
                    >
                      {copyStatus === 'copied'
                        ? 'Copied'
                        : copyStatus === 'error'
                          ? 'Copy failed'
                          : 'Copy brief'}
                    </button>
                    <button
                      className="secondary-button"
                      onClick={() => {
                        void downloadCoachingPdf(result, {
                          jobSnippet: jobDescription,
                        })
                      }}
                      type="button"
                    >
                      Download PDF
                    </button>
                  </div>
                </div>
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
                <p>
                  Paste a job description and résumé highlights, then download
                  the brief as a PDF when you are ready.
                </p>
              </div>
            )}
          </article>
        </section>
      )}

      {user && sessions.length > 0 && (
        <section className="history">
          <span className="eyebrow">Saved sessions</span>
          <h2>Your recent coaching history</h2>
          {historyError && <p className="error">{historyError}</p>}
          <ul>
            {sessions.map((session) => (
              <li key={session.id}>
                <div className="history-item">
                  <button
                    className="history-open"
                    onClick={() => {
                      setResult(session.result)
                      setSelectedSessionId(session.id)
                      setJobDescription(session.jobDescription)
                      setResumeHighlights(session.resumeHighlights)
                    }}
                    type="button"
                  >
                    <strong>{session.result.fitSummary}</strong>
                    <span>
                      {new Date(session.createdAt).toLocaleString()}
                    </span>
                  </button>
                  <button
                    className="history-delete"
                    disabled={deletingSessionId === session.id}
                    onClick={() => {
                      void handleDeleteSession(session.id)
                    }}
                    type="button"
                  >
                    {deletingSessionId === session.id ? 'Deleting…' : 'Delete'}
                  </button>
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}
    </main>
  )
}

function GoogleSignInButton({
  disabled,
  onCredential,
  onError,
}: {
  disabled: boolean
  onCredential: (credential: string) => void
  onError: (message: string) => void
}) {
  const buttonRef = useRef<HTMLDivElement>(null)
  const onCredentialRef = useRef(onCredential)
  const onErrorRef = useRef(onError)
  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined

  onCredentialRef.current = onCredential
  onErrorRef.current = onError

  useEffect(() => {
    if (!clientId) {
      return
    }

    let cancelled = false
    const initialize = () => {
      const google = window.google
      if (cancelled || !google || !buttonRef.current) {
        return
      }

      google.accounts.id.initialize({
        client_id: clientId,
        callback: (response) => onCredentialRef.current(response.credential),
      })
      buttonRef.current.replaceChildren()
      google.accounts.id.renderButton(buttonRef.current, {
        theme: 'outline',
        size: 'large',
        width: 280,
        text: 'continue_with',
      })
    }

    const existingScript = document.querySelector<HTMLScriptElement>(
      'script[data-google-identity]',
    )
    const script = existingScript ?? document.createElement('script')
    script.addEventListener('load', initialize)
    script.addEventListener('error', () =>
      onErrorRef.current('Unable to load Google sign-in.'),
    )

    if (!existingScript) {
      script.src = 'https://accounts.google.com/gsi/client'
      script.async = true
      script.dataset.googleIdentity = 'true'
      document.head.appendChild(script)
    } else if (window.google) {
      initialize()
    }

    return () => {
      cancelled = true
      script.removeEventListener('load', initialize)
    }
  }, [clientId])

  if (!clientId) {
    return (
      <p className="error">
        Set GOOGLE_CLIENT_ID in .env to enable Google sign-in.
      </p>
    )
  }

  return (
    <div
      className={disabled ? 'google-button disabled' : 'google-button'}
      ref={buttonRef}
    />
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
