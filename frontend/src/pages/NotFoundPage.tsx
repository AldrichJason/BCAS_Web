import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <section className="panel">
      <h1>Page not found</h1>
      <p>The page you were looking for does not exist or has been moved.</p>
      <Link className="button" to="/">
        Back to the portal
      </Link>
    </section>
  )
}
