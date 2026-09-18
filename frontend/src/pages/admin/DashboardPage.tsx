import { Link } from 'react-router-dom'
import { announcementsApi, chatbotApi, programsApi } from '../../api/endpoints'
import { useAuth } from '../../auth/useAuth'
import { useAsync } from '../../hooks/useAsync'

export function DashboardPage() {
  const { user } = useAuth()
  const announcements = useAsync((signal) => announcementsApi.list({ pageSize: 1, includeDrafts: true }, signal), [])
  const programs = useAsync((signal) => programsApi.list(true, signal), [])
  const faqs = useAsync((signal) => chatbotApi.faqs(true, signal), [])

  return (
    <section className="section">
      <header className="section__header">
        <h1>Content management</h1>
      </header>

      <p className="panel__lead">
        Signed in as {user?.fullName} ({user?.role}).
      </p>

      <div className="card-grid">
        <article className="card">
          <p className="card__meta">Announcements</p>
          <h2 className="card__stat">{announcements.data?.totalCount ?? '—'}</h2>
          <Link to="/admin/announcements">Manage announcements →</Link>
        </article>

        <article className="card">
          <p className="card__meta">Programs</p>
          <h2 className="card__stat">{programs.data?.length ?? '—'}</h2>
          <Link to="/programs">View public list →</Link>
        </article>

        <article className="card">
          <p className="card__meta">Chatbot FAQs</p>
          <h2 className="card__stat">{faqs.data?.length ?? '—'}</h2>
          <Link to="/admin/faqs">Manage the knowledge base →</Link>
        </article>
      </div>
    </section>
  )
}
