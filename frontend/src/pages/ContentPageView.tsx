import { useParams } from 'react-router-dom'
import { pagesApi } from '../api/endpoints'
import { ErrorMessage, Loading, RichText } from '../components/StatusMessage'
import { useAsync } from '../hooks/useAsync'

/** Renders any CMS page - about, admission, contact - from its slug. */
export function ContentPageView() {
  const { slug = '' } = useParams()
  const page = useAsync((signal) => pagesApi.bySlug(slug, signal), [slug])

  return (
    <article className="article">
      {page.loading && <Loading />}
      {page.error && <ErrorMessage message={page.error} onRetry={page.reload} />}

      {page.data && (
        <>
          <h1>{page.data.title}</h1>
          <RichText value={page.data.content} />
        </>
      )}
    </article>
  )
}
