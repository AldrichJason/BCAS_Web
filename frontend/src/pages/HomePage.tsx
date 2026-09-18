import { Link } from 'react-router-dom'
import { announcementsApi, programsApi } from '../api/endpoints'
import { ErrorMessage, Loading } from '../components/StatusMessage'
import { useAsync } from '../hooks/useAsync'
import { formatDate } from '../utils/format'

export function HomePage() {
  const news = useAsync((signal) => announcementsApi.list({ page: 1, pageSize: 3 }, signal), [])
  const programs = useAsync((signal) => programsApi.list(false, signal), [])

  return (
    <>
      <section className="hero">
        <p className="hero__eyebrow">Batangas College of Arts and Sciences</p>
        <h1>An education that opens doors in Batangas and beyond.</h1>
        <p className="hero__lead">
          Four baccalaureate programs, a faculty that knows every student by name, and a campus community that has served
          the province for three decades.
        </p>
        <div className="hero__actions">
          <Link className="button" to="/pages/admission">
            How to enroll
          </Link>
          <Link className="button button--ghost" to="/programs">
            Browse programs
          </Link>
        </div>
      </section>

      <section className="section">
        <header className="section__header">
          <h2>Latest news</h2>
          <Link to="/announcements">All announcements →</Link>
        </header>

        {news.loading && <Loading />}
        {news.error && <ErrorMessage message={news.error} onRetry={news.reload} />}

        <div className="card-grid">
          {news.data?.items.map((announcement) => (
            <article key={announcement.id} className="card">
              <p className="card__meta">
                {announcement.category} · {formatDate(announcement.publishedAt)}
              </p>
              <h3>
                <Link to={`/announcements/${announcement.slug}`}>{announcement.title}</Link>
              </h3>
              <p>{announcement.summary}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="section">
        <header className="section__header">
          <h2>Programs offered</h2>
          <Link to="/programs">Program details →</Link>
        </header>

        {programs.loading && <Loading />}
        {programs.error && <ErrorMessage message={programs.error} onRetry={programs.reload} />}

        <div className="card-grid">
          {programs.data?.map((program) => (
            <article key={program.id} className="card">
              <p className="card__meta">
                {program.code} · {program.durationYears} years
              </p>
              <h3>
                <Link to={`/programs/${program.slug}`}>{program.name}</Link>
              </h3>
              <p>{program.description.slice(0, 140)}…</p>
            </article>
          ))}
        </div>
      </section>
    </>
  )
}
