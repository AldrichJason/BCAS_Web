import { Link, useParams } from 'react-router-dom'
import { programsApi } from '../api/endpoints'
import { ErrorMessage, Loading, RichText } from '../components/StatusMessage'
import { useAsync } from '../hooks/useAsync'

export function ProgramDetailPage() {
  const { slug = '' } = useParams()
  const program = useAsync((signal) => programsApi.bySlug(slug, signal), [slug])

  return (
    <article className="article">
      <Link className="article__back" to="/programs">
        ← Back to programs
      </Link>

      {program.loading && <Loading />}
      {program.error && <ErrorMessage message={program.error} onRetry={program.reload} />}

      {program.data && (
        <>
          <p className="card__meta">
            {program.data.code} · {program.data.degreeLevel} · {program.data.durationYears} years
          </p>
          <h1>{program.data.name}</h1>
          <RichText value={program.data.description} />
          <Link className="button" to="/pages/admission">
            Admission requirements
          </Link>
        </>
      )}
    </article>
  )
}
