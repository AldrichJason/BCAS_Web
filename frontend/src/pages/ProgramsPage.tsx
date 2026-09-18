import { Link } from 'react-router-dom'
import { programsApi } from '../api/endpoints'
import { EmptyState, ErrorMessage, Loading } from '../components/StatusMessage'
import { useAsync } from '../hooks/useAsync'

export function ProgramsPage() {
  const programs = useAsync((signal) => programsApi.list(false, signal), [])

  return (
    <section className="section">
      <header className="section__header">
        <h1>Academic programs</h1>
      </header>

      {programs.loading && <Loading />}
      {programs.error && <ErrorMessage message={programs.error} onRetry={programs.reload} />}
      {programs.data?.length === 0 && <EmptyState message="No programs have been published yet." />}

      <div className="card-grid">
        {programs.data?.map((program) => (
          <article key={program.id} className="card">
            <p className="card__meta">
              {program.code} · {program.degreeLevel} · {program.durationYears} years
            </p>
            <h2>
              <Link to={`/programs/${program.slug}`}>{program.name}</Link>
            </h2>
            <p>{program.description}</p>
          </article>
        ))}
      </div>
    </section>
  )
}
