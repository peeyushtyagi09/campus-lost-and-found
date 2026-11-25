import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { ApiError } from '../api/http';
import { useAuth } from '../hooks/useAuth';

const registerSchema = z.object({
  displayName: z
    .string()
    .trim()
    .min(2, 'Display name must be at least 2 characters.')
    .max(60, 'Display name must be at most 60 characters.'),
  email: z.string().trim().min(1, 'Email is required.').email('Enter a valid email address.'),
  password: z
    .string()
    .min(10, 'Password must be at least 10 characters.')
    .max(128, 'Password must be at most 128 characters.')
    .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.')
    .regex(/[a-z]/, 'Password must contain at least one lowercase letter.')
    .regex(/[0-9]/, 'Password must contain at least one digit.'),
});

type RegisterFormValues = z.infer<typeof registerSchema>;

export function RegisterPage() {
  const { register: registerAccount } = useAuth();
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (values: RegisterFormValues) => {
    setServerError(null);
    try {
      await registerAccount(values);
      navigate('/login', { replace: true, state: { registered: true } });
    } catch (err) {
      if (err instanceof ApiError) {
        setServerError(err.message);
      } else {
        setServerError('Unable to create account right now.');
      }
    }
  };

  return (
    <section aria-labelledby="register-heading">
      <h1 id="register-heading">Create account</h1>
      {serverError ? (
        <p className="alert-error" role="alert">
          {serverError}
        </p>
      ) : null}
      <form className="form-card" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="field-group">
          <label className="field-label" htmlFor="register-display-name">
            Display name
          </label>
          <input
            id="register-display-name"
            type="text"
            autoComplete="name"
            className="field-input"
            aria-invalid={Boolean(errors.displayName)}
            aria-describedby={errors.displayName ? 'register-display-name-error' : undefined}
            {...register('displayName')}
          />
          {errors.displayName ? (
            <p id="register-display-name-error" className="field-error" role="alert">
              {errors.displayName.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="register-email">
            Campus email
          </label>
          <input
            id="register-email"
            type="email"
            autoComplete="email"
            className="field-input"
            aria-invalid={Boolean(errors.email)}
            aria-describedby={errors.email ? 'register-email-error' : undefined}
            {...register('email')}
          />
          {errors.email ? (
            <p id="register-email-error" className="field-error" role="alert">
              {errors.email.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="register-password">
            Password
          </label>
          <input
            id="register-password"
            type="password"
            autoComplete="new-password"
            className="field-input"
            aria-invalid={Boolean(errors.password)}
            aria-describedby="register-password-hint"
            {...register('password')}
          />
          <p id="register-password-hint" className="field-hint">
            At least 10 characters with uppercase, lowercase, and a digit.
          </p>
          {errors.password ? (
            <p id="register-password-error" className="field-error" role="alert">
              {errors.password.message}
            </p>
          ) : null}
        </div>

        <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Creating account...' : 'Create account'}
        </button>
      </form>
      <p>
        Already have an account? <Link to="/login">Sign in</Link>.
      </p>
    </section>
  );
}
