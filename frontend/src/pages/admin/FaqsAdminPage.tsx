import { useState, type FormEvent } from 'react'
import { chatbotApi } from '../../api/endpoints'
import type { Faq, FaqInput } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { EmptyState, ErrorMessage, Loading } from '../../components/StatusMessage'
import { describeError, useAsync } from '../../hooks/useAsync'

const emptyDraft: FaqInput = {
  question: '',
  answer: '',
  keywords: '',
  category: 'General',
  isActive: true,
}

export function FaqsAdminPage() {
  const { isAdministrator } = useAuth()
  const [editingId, setEditingId] = useState<number | null>(null)
  const [draft, setDraft] = useState<FaqInput>(emptyDraft)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const list = useAsync((signal) => chatbotApi.faqs(true, signal), [])

  function startCreate() {
    setEditingId(null)
    setDraft(emptyDraft)
    setFormError(null)
  }

  function startEdit(faq: Faq) {
    setEditingId(faq.id)
    setDraft({
      question: faq.question,
      answer: faq.answer,
      keywords: faq.keywords,
      category: faq.category,
      isActive: faq.isActive,
    })
    setFormError(null)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaving(true)
    setFormError(null)

    try {
      if (editingId === null) {
        await chatbotApi.createFaq(draft)
      } else {
        await chatbotApi.updateFaq(editingId, draft)
      }

      startCreate()
      list.reload()
    } catch (cause) {
      setFormError(describeError(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(faq: Faq) {
    if (!window.confirm(`Delete the FAQ "${faq.question}"?`)) {
      return
    }

    try {
      await chatbotApi.removeFaq(faq.id)

      if (editingId === faq.id) {
        startCreate()
      }

      list.reload()
    } catch (cause) {
      setFormError(describeError(cause))
    }
  }

  return (
    <section className="section">
      <header className="section__header">
        <h1>Chatbot knowledge base</h1>
        <button type="button" className="button button--ghost" onClick={startCreate}>
          New entry
        </button>
      </header>

      <p className="panel__lead">
        The assistant answers a visitor by matching their words against the keywords below, so list every phrasing a
        student might use.
      </p>

      <form className="form form--card" onSubmit={(event) => void handleSubmit(event)}>
        <h2>{editingId === null ? 'Add a FAQ' : 'Edit FAQ'}</h2>

        <label className="field">
          <span>Question</span>
          <input
            type="text"
            value={draft.question}
            required
            maxLength={300}
            onChange={(event) => setDraft({ ...draft, question: event.target.value })}
          />
        </label>

        <label className="field">
          <span>Answer</span>
          <textarea
            value={draft.answer}
            required
            rows={5}
            onChange={(event) => setDraft({ ...draft, answer: event.target.value })}
          />
        </label>

        <div className="field-row">
          <label className="field">
            <span>Keywords (comma separated)</span>
            <input
              type="text"
              value={draft.keywords}
              maxLength={500}
              placeholder="enroll, enrollment, requirements"
              onChange={(event) => setDraft({ ...draft, keywords: event.target.value })}
            />
          </label>

          <label className="field">
            <span>Category</span>
            <input
              type="text"
              value={draft.category}
              required
              maxLength={80}
              onChange={(event) => setDraft({ ...draft, category: event.target.value })}
            />
          </label>
        </div>

        <label className="field field--inline">
          <input
            type="checkbox"
            checked={draft.isActive}
            onChange={(event) => setDraft({ ...draft, isActive: event.target.checked })}
          />
          <span>Use this entry in the assistant</span>
        </label>

        {formError && (
          <p className="status status--error" role="alert">
            {formError}
          </p>
        )}

        <div className="form__actions">
          <button type="submit" className="button" disabled={saving}>
            {saving ? 'Saving…' : editingId === null ? 'Add entry' : 'Save changes'}
          </button>
          {editingId !== null && (
            <button type="button" className="button button--ghost" onClick={startCreate}>
              Cancel
            </button>
          )}
        </div>
      </form>

      {list.loading && <Loading />}
      {list.error && <ErrorMessage message={list.error} onRetry={list.reload} />}
      {list.data?.length === 0 && <EmptyState message="The knowledge base is empty." />}

      {list.data && list.data.length > 0 && (
        <table className="table">
          <thead>
            <tr>
              <th>Question</th>
              <th>Category</th>
              <th>Status</th>
              <th aria-label="Actions" />
            </tr>
          </thead>
          <tbody>
            {list.data.map((faq) => (
              <tr key={faq.id}>
                <td>{faq.question}</td>
                <td>{faq.category}</td>
                <td>
                  <span className={faq.isActive ? 'badge badge--live' : 'badge badge--draft'}>
                    {faq.isActive ? 'Active' : 'Hidden'}
                  </span>
                </td>
                <td className="table__actions">
                  <button type="button" className="button button--ghost" onClick={() => startEdit(faq)}>
                    Edit
                  </button>
                  {isAdministrator && (
                    <button type="button" className="button button--danger" onClick={() => void handleDelete(faq)}>
                      Delete
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  )
}
