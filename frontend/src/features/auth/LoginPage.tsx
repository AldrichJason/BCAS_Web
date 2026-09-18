import { useEffect, useRef, useState, type FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { ApiError } from '@/api/client';
import { useAuth } from './useAuth';
import { dashboardRouteForRole } from './roles';
import { hasErrors, validateLogin, type LoginFieldErrors } from './validation';
import './LoginPage.css';

export function LoginPage() {
  const { signIn, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fieldErrors, setFieldErrors] = useState<LoginFieldErrors>({});
  const [touched, setTouched] = useState<{ email: boolean; password: boolean }>({
    email: false,
    password: false,
  });
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const abortRef = useRef<AbortController | null>(null);

  // Someone already signed in has no business on the login page.
  useEffect(() => {
    if (user) {
      navigate(dashboardRouteForRole(user.roleCode), { replace: true });
    }
  }, [user, navigate]);

  useEffect(() => () => abortRef.current?.abort(), []);

  function handleBlur(field: 'email' | 'password') {
    setTouched((current) => ({ ...current, [field]: true }));
    setFieldErrors(validateLogin(email, password));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const errors = validateLogin(email, password);
    setFieldErrors(errors);
    setTouched({ email: true, password: true });
    if (hasErrors(errors)) {
      return;
    }

    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setIsSubmitting(true);
    try {
      const signedIn = await signIn(email.trim(), password, controller.signal);
      // Return the user to wherever they were headed before being bounced here.
      const from = (location.state as { from?: string } | null)?.from;
      navigate(from ?? dashboardRouteForRole(signedIn.roleCode), { replace: true });
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }
      setFormError(
        error instanceof ApiError ? error.message : 'Something went wrong. Please try again.',
      );
      setPassword('');
    } finally {
      setIsSubmitting(false);
    }
  }

  const emailError = touched.email ? fieldErrors.email : undefined;
  const passwordError = touched.password ? fieldErrors.password : undefined;

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={handleSubmit} noValidate>
        <header className="login-header">
          <h1>BCAS Portal</h1>
          <p>Sign in to manage your department&rsquo;s content.</p>
        </header>

        {formError && (
          <p className="login-error" role="alert">
            {formError}
          </p>
        )}

        <div className="login-field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            name="email"
            type="email"
            autoComplete="username"
            autoFocus
            value={email}
            disabled={isSubmitting}
            aria-invalid={emailError ? true : undefined}
            aria-describedby={emailError ? 'email-error' : undefined}
            onChange={(event) => setEmail(event.target.value)}
            onBlur={() => handleBlur('email')}
          />
          {emailError && (
            <span className="field-error" id="email-error">
              {emailError}
            </span>
          )}
        </div>

        <div className="login-field">
          <label htmlFor="password">Password</label>
          <input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            value={password}
            disabled={isSubmitting}
            aria-invalid={passwordError ? true : undefined}
            aria-describedby={passwordError ? 'password-error' : undefined}
            onChange={(event) => setPassword(event.target.value)}
            onBlur={() => handleBlur('password')}
          />
          {passwordError && (
            <span className="field-error" id="password-error">
              {passwordError}
            </span>
          )}
        </div>

        <button className="login-submit" type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </main>
  );
}
