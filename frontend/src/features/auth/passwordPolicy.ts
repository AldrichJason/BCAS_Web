/**
 * Mirror of the backend's PasswordPolicy (Features/Auth/PasswordPolicy.cs), used
 * for inline feedback only. The server's copy is the one that decides.
 */
export const PASSWORD_MIN_LENGTH = 10;

export const PASSWORD_POLICY_DESCRIPTION =
  'Password must be at least 10 characters and include an uppercase letter, a lowercase letter and a number.';

export function validatePassword(password: string): string | null {
  if (password.length < PASSWORD_MIN_LENGTH) {
    return PASSWORD_POLICY_DESCRIPTION;
  }

  const hasUpper = /[A-Z]/.test(password);
  const hasLower = /[a-z]/.test(password);
  const hasDigit = /[0-9]/.test(password);

  return hasUpper && hasLower && hasDigit ? null : PASSWORD_POLICY_DESCRIPTION;
}
