import { useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { announcementsApi } from '../api/endpoints'
import { EmptyState, ErrorMessage, Loading } from '../components/StatusMessage'
import { useAsync } from '../hooks/useAsync'
import { formatDate } from '../utils/format'

const PAGE_SIZE = 6

export function AnnouncementsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const page = Number(searchParams.get('page') ?? '1')
  const category = searchParams.get('category') ?? ''
  const search = searchParams.get('search') ?? ''
  const [draftSearch, setDraftSearch] = useState(search)

  const categories = useAsync((signal) => announcementsApi.categories(signal), [])
  const announcements = useAsync(
    (signal) => announcementsApi.list({ page, pageSize: PAGE_SIZE, category, search }, signal),
    [page, category, search],
  )

  function updateParams(changes: Record<string, string>) {
    const next = new URLSearchParams(searchParams)

    for (const [key, value] of Object.entries(changes)) {
      if (value) {
        next.set(key, value)
      } else {
        next.delete(key)
      }
    }

    setSearchParams(next)
  }

  const result = announcements.data

  return (
    <section className="section">
      <header className="section__header">
        <h1>News and announcements</h1>
      </header>

      <form
        className="filters"
        onSubmit={(event) => {
          event.preventDefault()
          updateParams({ search: draftSearch, page: '' })
        }}
      >
        <input
          type="search"
          value={draftSearch}
          placeholder="Search announcements"
          aria-label="Search announcements"
          onChange={(event) => setDraftSearch(event.target.value)}
        />

        <select
          value={category}
          aria-label="Filter by category"
          onChange={(event) => updateParams({ category: event.target.value, page: '' })}
        >
          <option value="">All categories</option>
          {categories.data?.map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </select>

        <button type="submit" className="button">
          Search
        </button>
      </form>

      {announcements.loading && <Loading />}
      {announcements.error && <ErrorMessage message={announcements.error} onRetry={announcements.reload} />}
      {result && result.items.length === 0 && <EmptyState message="No announcements match your filters yet." />}

      <div className="card-grid">
        {result?.items.map((announcement) => (
          <article key={announcement.id} className="card">
            <p className="card__meta">
              {announcement.category} · {formatDate(announcement.publishedAt)}
            </p>
            <h2>
              <Link to={`/announcements/${announcement.slug}`}>{announcement.title}</Link>
            </h2>
            <p>{announcement.summary}</p>
          </article>
        ))}
      </div>

      {result && result.totalPages > 1 && (
        <nav className="pagination" aria-label="Pagination">
          <button
            type="button"
            className="button button--ghost"
            disabled={result.page <= 1}
            onClick={() => updateParams({ page: String(result.page - 1) })}
          >
            Previous
          </button>
          <span>
            Page {result.page} of {result.totalPages}
          </span>
          <button
            type="button"
            className="button button--ghost"
            disabled={result.page >= result.totalPages}
            onClick={() => updateParams({ page: String(result.page + 1) })}
          >
            Next
          </button>
        </nav>
      )}
    </section>
  )
}
