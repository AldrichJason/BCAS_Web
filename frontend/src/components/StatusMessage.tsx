export function Loading({ label = 'Loading…' }: { label?: string }) {
  return (
    <p className="status status--loading" role="status">
      {label}
    </p>
  )
}

export function ErrorMessage({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="status status--error" role="alert">
      <span>{message}</span>
      {onRetry && (
        <button type="button" className="button button--ghost" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}

export function EmptyState({ message }: { message: string }) {
  return <p className="status status--empty">{message}</p>
}

/** Renders plain text content as paragraphs - never as HTML, so stored text cannot inject markup. */
export function RichText({ value }: { value: string }) {
  const paragraphs = value.split(/\n{2,}/).filter((paragraph) => paragraph.trim().length > 0)

  return (
    <div className="rich-text">
      {paragraphs.map((paragraph, index) => (
        <p key={index}>{paragraph}</p>
      ))}
    </div>
  )
}
