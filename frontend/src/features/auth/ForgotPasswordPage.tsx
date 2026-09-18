import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { forgotPassword } from '@/api/auth';
import { ApiError } from '@/api/client';
import './AuthForms.css';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [emailError, setEmailError] = useState<string | undefined>();
  const [touched, setTouched] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [confirmation, setConfirmation] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const abortRef = useRef<AbortController | null>(null);
  useEffect(() => () => abortRef.current?.abort(), []);

  function validate(value: string): string | undefined {
    const trimmed = value.trim();
    if (trimmed.length === 0) {
      return 'Email is required.';
    }
    return EMAIL_PATTERN.test(trimmed) ? undefined : 'Enter a valid email address.';
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const error = validate(email);
    setEmailError(error);
    setTouched(true);
    if (error) {
      return;
    }

    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsSubmitting(true);
    try {
      const response = await forgotPassword(email.trim(), controller.signal);
      // The message is deliberately the same whether or not the account exists.
      setConfirmation(response.message);
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }
      setFormError(
        error instanceof ApiError ? error.message : 'Something went wrong. Please try again.',
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  if (confirmation) {
    return (
      <main className="auth-page">
        <section className="auth-card">
          <header className="auth-header">
            <h1>Check your email</h1>
          </header>
          <p className="auth-success" role="status">
            {confirmation}
          </p>
          <p className="auth-hint">
            The link can be used once and expires after a short while. If it does not arrive,
            check your spam folder or request another.
          </p>
          <Link className="auth-link" to="/login">
            Back to sign in
          </Link>
        </section>
      </main>
    );
  }

  return (
    <main className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit} noValidate>
        <header className="auth-header">
          <h1>Forgot your password?</h1>
          <p>Enter your email and we&rsquo;ll send you a link to choose a new one.</p>
        </header>

        {formError && (
          <p className="auth-error" role="alert">
            {formError}
          </p>
        )}

        <div className="auth-field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            name="email"
            type="email"
            autoComplete="username"
            autoFocus
            value={email}
            disabled={isSubmitting}
            aria-invalid={touched && emailError ? true : undefined}
            aria-describedby={touched && emailError ? 'email-error' : undefined}
            onChange={(event) => setEmail(event.target.value)}
            onBlur={() => {
              setTouched(true);
              setEmailError(validate(email));
            }}
          />
          {touched && emailError && (
            <span className="field-error" id="email-error">
              {emailError}
            </span>
          )}
        </div>

        <button className="auth-submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Sending…' : 'Send reset link'}
        </button>

        <Link className="auth-link" to="/login">
          Back to sign in
        </Link>
      </form>
    </main>
  );
}
