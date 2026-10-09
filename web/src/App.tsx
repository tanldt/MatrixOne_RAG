import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import { getDocuments, runIngestion, sendChat } from './api'
import type { Citation, ChatResponse, IndexedDocument } from './api'

type Message = {
  role: 'user' | 'assistant'
  content: string
  citations?: Citation[]
}

const exampleQuestions = [
  'A customer says their ticket transfer email never arrived. What should I check first?',
  'When is a refund allowed for a delayed ticket order?',
  'Write a short reply for a customer whose order is delayed but still expected to be delivered.',
  'When should support escalate an order issue to operations?',
]

function App() {
  const [documents, setDocuments] = useState<IndexedDocument[]>([])
  const [messages, setMessages] = useState<Message[]>([])
  const [question, setQuestion] = useState('')
  const [selectedCitations, setSelectedCitations] = useState<Citation[]>([])
  const [status, setStatus] = useState('Ready')
  const [isBusy, setIsBusy] = useState(false)

  const totalChunks = useMemo(
    () => documents.reduce((total, document) => total + document.chunkCount, 0),
    [documents],
  )

  useEffect(() => {
    refreshDocuments()
  }, [])

  async function refreshDocuments() {
    try {
      setDocuments(await getDocuments())
    } catch {
      setDocuments([])
    }
  }

  async function handleIngest() {
    setIsBusy(true)
    setStatus('Indexing sample docs')
    try {
      const result = await runIngestion()
      setStatus(`Indexed ${result.documentCount} docs and ${result.chunkCount} chunks`)
      await refreshDocuments()
    } catch (error) {
      setStatus(error instanceof Error ? error.message : 'Ingestion failed')
    } finally {
      setIsBusy(false)
    }
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    const trimmed = question.trim()
    if (!trimmed || isBusy) return

    setQuestion('')
    setMessages((current) => [...current, { role: 'user', content: trimmed }])
    setIsBusy(true)
    setStatus('Retrieving context')

    try {
      const response: ChatResponse = await sendChat(trimmed)
      setMessages((current) => [
        ...current,
        { role: 'assistant', content: response.answer, citations: response.citations },
      ])
      setSelectedCitations(response.citations)
      setStatus('Answer ready')
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Chat request failed'
      setMessages((current) => [...current, { role: 'assistant', content: message, citations: [] }])
      setSelectedCitations([])
      setStatus(message)
    } finally {
      setIsBusy(false)
    }
  }

  return (
    <main className="app-shell">
      <section className="workspace">
        <header className="topbar">
          <div>
            <p className="eyebrow">Ticketshine Support Copilot</p>
            <h1>Grounded answers for order support</h1>
          </div>
          <button className="primary-action" type="button" onClick={handleIngest} disabled={isBusy}>
            Index docs
          </button>
        </header>

        <div className="status-strip" aria-live="polite">
          <span>{status}</span>
          <span>{documents.length} docs</span>
          <span>{totalChunks} chunks</span>
        </div>

        <div className="content-grid">
          <section className="chat-panel" aria-label="Support chat">
            <div className="messages">
              {messages.length === 0 ? (
                <div className="empty-state">
                  <h2>Ask a support question</h2>
                  <p>Use the sample Ticketshine policies to test retrieval, answer grounding, and citations.</p>
                </div>
              ) : (
                messages.map((message, index) => (
                  <article className={`message ${message.role}`} key={`${message.role}-${index}`}>
                    <p>{message.content}</p>
                    {message.citations && message.citations.length > 0 ? (
                      <button type="button" onClick={() => setSelectedCitations(message.citations ?? [])}>
                        View citations
                      </button>
                    ) : null}
                  </article>
                ))
              )}
            </div>

            <div className="examples" aria-label="Example questions">
              {exampleQuestions.map((example) => (
                <button key={example} type="button" onClick={() => setQuestion(example)} disabled={isBusy}>
                  {example}
                </button>
              ))}
            </div>

            <form className="composer" onSubmit={handleSubmit}>
              <textarea
                value={question}
                onChange={(event) => setQuestion(event.target.value)}
                placeholder="Ask about refunds, delayed orders, transfers, or escalation..."
                rows={3}
              />
              <button type="submit" disabled={isBusy || !question.trim()}>
                Send
              </button>
            </form>
          </section>

          <aside className="side-panel" aria-label="Sources and indexed documents">
            <section>
              <h2>Citations</h2>
              {selectedCitations.length === 0 ? (
                <p className="muted">Citations appear after an answer.</p>
              ) : (
                <ul className="citation-list">
                  {selectedCitations.map((citation, index) => (
                    <li key={`${citation.documentName}-${citation.chunkIndex}-${index}`}>
                      <strong>{citation.documentName}</strong>
                      <span>{citation.heading || 'Untitled section'}</span>
                      <small>chunk {citation.chunkIndex}</small>
                    </li>
                  ))}
                </ul>
              )}
            </section>

            <section>
              <h2>Indexed Docs</h2>
              {documents.length === 0 ? (
                <p className="muted">Run indexing after the API and MatrixOne are available.</p>
              ) : (
                <ul className="doc-list">
                  {documents.map((document) => (
                    <li key={document.id}>
                      <span>{document.name}</span>
                      <small>{document.chunkCount} chunks</small>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </aside>
        </div>
      </section>
    </main>
  )
}

export default App
