import { Link, useParams } from 'react-router-dom'
import { announcementsApi } from '../api/endpoints'
import { ErrorMessage, Loading, RichText } from '../components/StatusMessage'
import { useAsync } from '../hooks/useAsync'
import { formatDate } from '../utils/format'

export function AnnouncementDetailPage() {
  const { slug = '' } = useParams()
  const announcement = useAsync((signal) => announcementsApi.bySlug(slug, signal), [slug])

  return (
    <article className="article">
      <Link className="article__back" to="/announcements">
        ← Back to news
      </Link>

      {announcement.loading && <Loading />}
      {announcement.error && <ErrorMessage message={announcement.error} onRetry={announcement.reload} />}

      {announcement.data && (
        <>
          <p className="card__meta">
            {announcement.data.category} · {formatDate(announcement.data.publishedAt)} · by {announcement.data.authorName}
          </p>
          <h1>{announcement.data.title}</h1>
          {!announcement.data.isPublished && <p className="badge badge--draft">Draft</p>}
          {announcement.data.imageUrl && (
            <img className="article__image" src={announcement.data.imageUrl} alt="" loading="lazy" />
          )}
          <p className="article__summary">{announcement.data.summary}</p>
          <RichText value={announcement.data.content} />
        </>
      )}
    </article>
  )
}
