import { useState, type FormEvent } from 'react'
import { announcementsApi } from '../../api/endpoints'
import type { Announcement, AnnouncementInput } from '../../api/types'
import { useAuth } from '../../auth/useAuth'
import { EmptyState, ErrorMessage, Loading } from '../../components/StatusMessage'
import { describeError, useAsync } from '../../hooks/useAsync'
import { formatDate } from '../../utils/format'

const emptyDraft: AnnouncementInput = {
  title: '',
  summary: '',
  content: '',
  imageUrl: null,
  category: 'Announcement',
  isPublished: false,
}

function toDraft(announcement: Announcement): AnnouncementInput {
  return {
    title: announcement.title,
    summary: announcement.summary,
    content: announcement.content,
    imageUrl: announcement.imageUrl,
    category: announcement.category,
    isPublished: announcement.isPublished,
  }
}

export function AnnouncementsAdminPage() {
  const { isAdministrator } = useAuth()
  const [editingId, setEditingId] = useState<number | null>(null)
  const [draft, setDraft] = useState<AnnouncementInput>(emptyDraft)
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const list = useAsync(
    (signal) => announcementsApi.list({ page: 1, pageSize: 50, includeDrafts: true }, signal),
    [],
  )

  function startCreate() {
    setEditingId(null)
    setDraft(emptyDraft)
    setFormError(null)
  }

  function startEdit(announcement: Announcement) {
    setEditingId(announcement.id)
    setDraft(toDraft(announcement))
    setFormError(null)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaving(true)
    setFormError(null)

    const payload: AnnouncementInput = {
      ...draft,
      imageUrl: draft.imageUrl && draft.imageUrl.trim().length > 0 ? draft.imageUrl.trim() : null,
    }

    try {
      if (editingId === null) {
        await announcementsApi.create(payload)
      } else {
        await announcementsApi.update(editingId, payload)
      }

      startCreate()
      list.reload()
    } catch (cause) {
      setFormError(describeError(cause))
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(announcement: Announcement) {
    if (!window.confirm(`Delete "${announcement.title}"? This cannot be undone.`)) {
      return
    }

    try {
      await announcementsApi.remove(announcement.id)

      if (editingId === announcement.id) {
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
        <h1>Announcements</h1>
        <button type="button" className="button button--ghost" onClick={startCreate}>
          New announcement
        </button>
      </header>

      <form className="form form--card" onSubmit={(event) => void handleSubmit(event)}>
        <h2>{editingId === null ? 'Create an announcement' : 'Edit announcement'}</h2>

        <label className="field">
          <span>Title</span>
          <input
            type="text"
            value={draft.title}
            required
            maxLength={200}
            onChange={(event) => setDraft({ ...draft, title: event.target.value })}
          />
        </label>

        <label className="field">
          <span>Summary</span>
          <input
            type="text"
            value={draft.summary}
            required
            maxLength={500}
            onChange={(event) => setDraft({ ...draft, summary: event.target.value })}
          />
        </label>

        <label className="field">
          <span>Content</span>
          <textarea
            value={draft.content}
            required
            rows={8}
            onChange={(event) => setDraft({ ...draft, content: event.target.value })}
          />
        </label>

        <div className="field-row">
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

          <label className="field">
            <span>Image URL (optional)</span>
            <input
              type="url"
              value={draft.imageUrl ?? ''}
              maxLength={500}
              onChange={(event) => setDraft({ ...draft, imageUrl: event.target.value })}
            />
          </label>
        </div>

        <label className="field field--inline">
          <input
            type="checkbox"
            checked={draft.isPublished}
            onChange={(event) => setDraft({ ...draft, isPublished: event.target.checked })}
          />
          <span>Publish on the portal</span>
        </label>

        {formError && (
          <p className="status status--error" role="alert">
            {formError}
          </p>
        )}

        <div className="form__actions">
          <button type="submit" className="button" disabled={saving}>
            {saving ? 'Saving…' : editingId === null ? 'Create' : 'Save changes'}
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
      {list.data?.items.length === 0 && <EmptyState message="No announcements yet. Create the first one above." />}

      {list.data && list.data.items.length > 0 && (
        <table className="table">
          <thead>
            <tr>
              <th>Title</th>
              <th>Category</th>
              <th>Status</th>
              <th>Published</th>
              <th aria-label="Actions" />
            </tr>
          </thead>
          <tbody>
            {list.data.items.map((announcement) => (
              <tr key={announcement.id}>
                <td>{announcement.title}</td>
                <td>{announcement.category}</td>
                <td>
                  <span className={announcement.isPublished ? 'badge badge--live' : 'badge badge--draft'}>
                    {announcement.isPublished ? 'Published' : 'Draft'}
                  </span>
                </td>
                <td>{formatDate(announcement.publishedAt)}</td>
                <td className="table__actions">
                  <button type="button" className="button button--ghost" onClick={() => startEdit(announcement)}>
                    Edit
                  </button>
                  {isAdministrator && (
                    <button
                      type="button"
                      className="button button--danger"
                      onClick={() => void handleDelete(announcement)}
                    >
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
