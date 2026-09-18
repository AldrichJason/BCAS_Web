import { useMemo, useState, type FormEvent } from 'react';
import { createAccount, type CreateAccountInput } from '@/api/accounts';
import { ApiError } from '@/api/client';
import type { AccountReference, UserAccount } from '@/api/types';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface FieldErrors {
  firstName?: string;
  lastName?: string;
  email?: string;
  roleCode?: string;
  departmentId?: string;
}

interface CreateAccountFormProps {
  token: string;
  reference: AccountReference;
  onCreated: (account: UserAccount) => void;
}

export function CreateAccountForm({ token, reference, onCreated }: CreateAccountFormProps) {
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [roleCode, setRoleCode] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [errors, setErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Only the Academic Head is department-scoped; the API rejects a department
  // sent for any other role, so the field is hidden rather than ignored.
  const needsDepartment = useMemo(
    () => reference.roles.find((r) => r.code === roleCode)?.requiresDepartment ?? false,
    [reference.roles, roleCode],
  );

  function validate(): FieldErrors {
    const next: FieldErrors = {};

    if (firstName.trim().length === 0) {
      next.firstName = 'First name is required.';
    }
    if (lastName.trim().length === 0) {
      next.lastName = 'Last name is required.';
    }
    if (email.trim().length === 0) {
      next.email = 'Email is required.';
    } else if (!EMAIL_PATTERN.test(email.trim())) {
      next.email = 'Enter a valid email address.';
    }
    if (roleCode.length === 0) {
      next.roleCode = 'Choose a role.';
    }
    if (needsDepartment && departmentId.length === 0) {
      next.departmentId = 'An Academic Head must be assigned a department.';
    }

    return next;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const found = validate();
    setErrors(found);
    if (Object.keys(found).length > 0) {
      return;
    }

    const input: CreateAccountInput = {
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      email: email.trim(),
      roleCode,
      departmentId: needsDepartment ? Number(departmentId) : null,
    };

    setIsSubmitting(true);
    try {
      const created = await createAccount(token, input);
      onCreated(created);
      setFirstName('');
      setLastName('');
      setEmail('');
      setRoleCode('');
      setDepartmentId('');
      setErrors({});
    } catch (error) {
      if (error instanceof ApiError && error.code === 'duplicate_email') {
        setErrors({ email: error.message });
      } else {
        setFormError(
          error instanceof ApiError ? error.message : 'Something went wrong. Please try again.',
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="account-form" onSubmit={handleSubmit} noValidate>
      <h2>Add an account</h2>

      {formError && (
        <p className="auth-error" role="alert">
          {formError}
        </p>
      )}

      <div className="account-form-row">
        <div className="auth-field">
          <label htmlFor="firstName">First name</label>
          <input
            id="firstName"
            value={firstName}
            disabled={isSubmitting}
            aria-invalid={errors.firstName ? true : undefined}
            onChange={(event) => setFirstName(event.target.value)}
          />
          {errors.firstName && <span className="field-error">{errors.firstName}</span>}
        </div>

        <div className="auth-field">
          <label htmlFor="lastName">Last name</label>
          <input
            id="lastName"
            value={lastName}
            disabled={isSubmitting}
            aria-invalid={errors.lastName ? true : undefined}
            onChange={(event) => setLastName(event.target.value)}
          />
          {errors.lastName && <span className="field-error">{errors.lastName}</span>}
        </div>
      </div>

      <div className="auth-field">
        <label htmlFor="email">Email</label>
        <input
          id="email"
          type="email"
          value={email}
          disabled={isSubmitting}
          aria-invalid={errors.email ? true : undefined}
          onChange={(event) => setEmail(event.target.value)}
        />
        {errors.email && <span className="field-error">{errors.email}</span>}
      </div>

      <div className="account-form-row">
        <div className="auth-field">
          <label htmlFor="roleCode">Role</label>
          <select
            id="roleCode"
            value={roleCode}
            disabled={isSubmitting}
            aria-invalid={errors.roleCode ? true : undefined}
            onChange={(event) => {
              setRoleCode(event.target.value);
              setDepartmentId('');
            }}
          >
            <option value="">Choose a role&hellip;</option>
            {reference.roles.map((role) => (
              <option key={role.code} value={role.code}>
                {role.name}
              </option>
            ))}
          </select>
          {errors.roleCode && <span className="field-error">{errors.roleCode}</span>}
        </div>

        {needsDepartment && (
          <div className="auth-field">
            <label htmlFor="departmentId">Department</label>
            <select
              id="departmentId"
              value={departmentId}
              disabled={isSubmitting}
              aria-invalid={errors.departmentId ? true : undefined}
              onChange={(event) => setDepartmentId(event.target.value)}
            >
              <option value="">Choose a department&hellip;</option>
              {reference.departments.map((department) => (
                <option key={department.departmentId} value={department.departmentId}>
                  {department.name}
                </option>
              ))}
            </select>
            {errors.departmentId && <span className="field-error">{errors.departmentId}</span>}
          </div>
        )}
      </div>

      <p className="auth-hint">
        The new user is emailed a link to set their own password. No password is set here.
      </p>

      <button className="auth-submit" type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Creating…' : 'Create account and send invitation'}
      </button>
    </form>
  );
}
