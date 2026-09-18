import { useEffect, useRef, useState } from 'react'
import { chatbotApi } from '../api/endpoints'
import { describeError } from '../hooks/useAsync'

interface Bubble {
  id: number
  author: 'visitor' | 'bot'
  text: string
}

const GREETING: Bubble = {
  id: 0,
  author: 'bot',
  text: 'Hi! I am the BCAS assistant. Ask me about enrollment, programs, tuition or requirements.',
}

/** Floating assistant backed by the FAQ knowledge base in SQL Server. */
export function ChatWidget() {
  const [open, setOpen] = useState(false)
  const [bubbles, setBubbles] = useState<Bubble[]>([GREETING])
  const [draft, setDraft] = useState('')
  const [sending, setSending] = useState(false)
  const [suggestions, setSuggestions] = useState<string[]>([])
  const sessionId = useRef<string | null>(null)
  const transcript = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    transcript.current?.scrollTo({ top: transcript.current.scrollHeight })
  }, [bubbles, open])

  async function ask(message: string) {
    const question = message.trim()

    if (!question || sending) {
      return
    }

    setDraft('')
    setSuggestions([])
    setBubbles((current) => [...current, { id: Date.now(), author: 'visitor', text: question }])
    setSending(true)

    try {
      const reply = await chatbotApi.ask(question, sessionId.current)
      sessionId.current = reply.sessionId

      setBubbles((current) => [...current, { id: Date.now() + 1, author: 'bot', text: reply.reply }])
      setSuggestions(reply.suggestions)
    } catch (error) {
      setBubbles((current) => [...current, { id: Date.now() + 1, author: 'bot', text: describeError(error) }])
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="chat">
      {open && (
        <section className="chat__panel" aria-label="BCAS assistant">
          <header className="chat__header">
            <span>BCAS Assistant</span>
            <button type="button" className="chat__close" onClick={() => setOpen(false)} aria-label="Close the assistant">
              ×
            </button>
          </header>

          <div className="chat__transcript" ref={transcript}>
            {bubbles.map((bubble) => (
              <p key={bubble.id} className={`chat__bubble chat__bubble--${bubble.author}`}>
                {bubble.text}
              </p>
            ))}
            {sending && <p className="chat__bubble chat__bubble--bot chat__bubble--typing">Typing…</p>}
          </div>

          {suggestions.length > 0 && (
            <div className="chat__suggestions">
              {suggestions.map((suggestion) => (
                <button key={suggestion} type="button" className="chat__suggestion" onClick={() => void ask(suggestion)}>
                  {suggestion}
                </button>
              ))}
            </div>
          )}

          <form
            className="chat__form"
            onSubmit={(event) => {
              event.preventDefault()
              void ask(draft)
            }}
          >
            <input
              type="text"
              value={draft}
              maxLength={500}
              placeholder="Type your question…"
              aria-label="Your question"
              onChange={(event) => setDraft(event.target.value)}
            />
            <button type="submit" className="button" disabled={sending || draft.trim().length === 0}>
              Send
            </button>
          </form>
        </section>
      )}

      <button type="button" className="chat__launcher" onClick={() => setOpen((value) => !value)}>
        {open ? 'Close chat' : 'Ask BCAS'}
      </button>
    </div>
  )
}
