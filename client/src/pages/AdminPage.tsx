import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { createStaffAccount, getAdminUsers, getAuditLogs } from '../api/admin';
import { ApiError } from '../api/http';
import styles from './BoardPage.module.css';

const createStaffSchema = z.object({
  displayName: z.string().trim().min(2, 'Display name is required.').max(60),
  email: z.string().trim().email('Valid email is required.'),
  password: z
    .string()
    .min(10, 'Password must be at least 10 characters.')
    .regex(/[A-Z]/, 'Must include uppercase.')
    .regex(/[a-z]/, 'Must include lowercase.')
    .regex(/[0-9]/, 'Must include digit.'),
});

type CreateStaffFormValues = z.infer<typeof createStaffSchema>;

export function AdminPage() {
  const queryClient = useQueryClient();
  const [serverError, setServerError] = useState<string | null>(null);

  const usersQuery = useQuery({
    queryKey: ['admin-users'],
    queryFn: getAdminUsers,
  });

  const auditQuery = useQuery({
    queryKey: ['admin-audit'],
    queryFn: () => getAuditLogs(1, 25),
  });

  const createMutation = useMutation({
    mutationFn: createStaffAccount,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      void queryClient.invalidateQueries({ queryKey: ['admin-audit'] });
    },
    onError: (err) => setServerError(err instanceof ApiError ? err.message : 'Action failed.'),
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateStaffFormValues>({
    resolver: zodResolver(createStaffSchema),
  });

  const onSubmit = async (values: CreateStaffFormValues) => {
    setServerError(null);
    await createMutation.mutateAsync(values);
    reset();
  };

  return (
    <section aria-labelledby="admin-heading">
      <h1 id="admin-heading">Administration</h1>

      <h2>Create staff account</h2>
      {serverError ? (
        <p className="alert-error" role="alert">
          {serverError}
        </p>
      ) : null}
      <form className="form-card" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="field-group">
          <label className="field-label" htmlFor="staff-name">
            Display name
          </label>
          <input id="staff-name" type="text" className="field-input" {...register('displayName')} />
          {errors.displayName ? (
            <p className="field-error" role="alert">
              {errors.displayName.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="staff-email">
            Staff email
          </label>
          <input id="staff-email" type="email" className="field-input" {...register('email')} />
          {errors.email ? (
            <p className="field-error" role="alert">
              {errors.email.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="staff-password">
            Temporary password
          </label>
          <input
            id="staff-password"
            type="password"
            className="field-input"
            {...register('password')}
          />
          {errors.password ? (
            <p className="field-error" role="alert">
              {errors.password.message}
            </p>
          ) : null}
        </div>

        <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
          Create staff account
        </button>
      </form>

      <h2>Accounts</h2>
      {usersQuery.isLoading ? (
        <p role="status">Loading accounts...</p>
      ) : usersQuery.data ? (
        <ul className={styles.list}>
          {usersQuery.data.map((u) => (
            <li key={u.id} className={styles.reportCard}>
              <strong>{u.displayName}</strong> &middot; {u.role}
            </li>
          ))}
        </ul>
      ) : null}

      <h2>Audit log (read-only)</h2>
      {auditQuery.isLoading ? (
        <p role="status">Loading audit logs...</p>
      ) : auditQuery.data ? (
        <ul className={styles.list}>
          {auditQuery.data.items.map((evt) => (
            <li key={evt.id} className={styles.reportCard}>
              <strong>{evt.action}</strong> ({evt.actorRole}) &middot; {evt.summary}
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
