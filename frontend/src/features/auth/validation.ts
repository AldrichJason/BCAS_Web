export interface LoginFieldErrors {
  email?: string;
  password?: string;
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Client-side mirror of the API's LoginRequest validation, for inline feedback. */
export function validateLogin(email: string, password: string): LoginFieldErrors {
  const errors: LoginFieldErrors = {};

  const trimmedEmail = email.trim();
  if (trimmedEmail.length === 0) {
    errors.email = 'Email is required.';
  } else if (!EMAIL_PATTERN.test(trimmedEmail)) {
    errors.email = 'Enter a valid email address.';
  }

  if (password.length === 0) {
    errors.password = 'Password is required.';
  }

  return errors;
}

export function hasErrors(errors: LoginFieldErrors): boolean {
  return Object.keys(errors).length > 0;
}
