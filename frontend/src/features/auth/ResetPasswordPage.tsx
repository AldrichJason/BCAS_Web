import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { resetPassword } from '@/api/auth';
import { ApiError } from '@/api/client';
import { PASSWORD_POLICY_DESCRIPTION, validatePassword } from './passwordPolicy';
import './AuthForms.css';

interface FieldErrors {
  newPassword?: string;
  confirmPassword?: string;
}

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token') ?? '';

  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [touched, setTouched] = useState({ newPassword: false, confirmPassword: false });
  const [formError, setFormError] = useState<string | null>(null);
  /** True when the link itself is the problem, so we offer a fresh one. */
  const [isLinkDead, setIsLinkDead] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const abortRef = useRef<AbortController | null>(null);
  useEffect(() => () => abortRef.current?.abort(), []);

  function validate(password: string, confirmation: string): FieldErrors {
    const errors: FieldErrors = {};

    const policyError = validatePassword(password);
    if (policyError) {
      errors.newPassword = policyError;
    }

    if (confirmation.length === 0) {
      errors.confirmPassword = 'Confirm your new password.';
    } else if (confirmation !== password) {
      errors.confirmPassword = 'The two passwords do not match.';
    }

    return errors;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const errors = validate(newPassword, confirmPassword);
    setFieldErrors(errors);
    setTouched({ newPassword: true, confirmPassword: true });
    if (Object.keys(errors).length > 0) {
      return;
    }

    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsSubmitting(true);
    try {
      await resetPassword(token, newPassword, confirmPassword, controller.signal);
      navigate('/login', {
        replace: true,
        state: { notice: 'Your password has been changed. Sign in with your new password.' },
      });
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }
      if (error instanceof ApiError) {
        setIsLinkDead(error.code === 'invalid_reset_token');
        setFormError(error.message);
      } else {
        setFormError('Something went wrong. Please try again.');
      }
      setNewPassword('');
      setConfirmPassword('');
    } finally {
      setIsSubmitting(false);
    }
  }

  // No token in the URL at all: same dead end as a used or expired link.
  if (token.length === 0 || isLinkDead) {
    return (
      <main className="auth-page">
        <section className="auth-card">
          <header className="auth-header">
            <h1>This link is no longer valid</h1>
          </header>
          <p className="auth-error" role="alert">
            {formError ??
              'This reset link is missing or has already been used. Reset links can be used once and expire after a short while.'}
          </p>
          <Link className="auth-submit auth-submit-link" to="/forgot-password">
            Request a new link
          </Link>
          <Link className="auth-link" to="/login">
            Back to sign in
          </Link>
        </section>
      </main>
    );
  }

  const newPasswordError = touched.newPassword ? fieldErrors.newPassword : undefined;
  const confirmPasswordError = touched.confirmPassword ? fieldErrors.confirmPassword : undefined;

  return (
    <main className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit} noValidate>
        <header className="auth-header">
          <h1>Choose a new password</h1>
          <p>{PASSWORD_POLICY_DESCRIPTION}</p>
        </header>

        {formError && (
          <p className="auth-error" role="alert">
            {formError}
          </p>
        )}

        <div className="auth-field">
          <label htmlFor="newPassword">New password</label>
          <input
            id="newPassword"
            name="newPassword"
            type="password"
            autoComplete="new-password"
            autoFocus
            value={newPassword}
            disabled={isSubmitting}
            aria-invalid={newPasswordError ? true : undefined}
            aria-describedby={newPasswordError ? 'new-password-error' : undefined}
            onChange={(event) => setNewPassword(event.target.value)}
            onBlur={() => {
              setTouched((current) => ({ ...current, newPassword: true }));
              setFieldErrors(validate(newPassword, confirmPassword));
            }}
          />
          {newPasswordError && (
            <span className="field-error" id="new-password-error">
              {newPasswordError}
            </span>
          )}
        </div>

        <div className="auth-field">
          <label htmlFor="confirmPassword">Confirm new password</label>
          <input
            id="confirmPassword"
            name="confirmPassword"
            type="password"
            autoComplete="new-password"
            value={confirmPassword}
            disabled={isSubmitting}
            aria-invalid={confirmPasswordError ? true : undefined}
            aria-describedby={confirmPasswordError ? 'confirm-password-error' : undefined}
            onChange={(event) => setConfirmPassword(event.target.value)}
            onBlur={() => {
              setTouched((current) => ({ ...current, confirmPassword: true }));
              setFieldErrors(validate(newPassword, confirmPassword));
            }}
          />
          {confirmPasswordError && (
            <span className="field-error" id="confirm-password-error">
              {confirmPasswordError}
            </span>
          )}
        </div>

        <button className="auth-submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : 'Change password'}
        </button>

        <Link className="auth-link" to="/login">
          Back to sign in
        </Link>
      </form>
    </main>
  );
}
