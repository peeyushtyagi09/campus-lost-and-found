import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { resolveApiUrl } from '../api/http';
import { closeLostReport, getMyLostReports } from '../api/lostReports';
import { CATEGORY_LABELS } from '../types/common';
import styles from './BoardPage.module.css';

export function MyReportsPage() {
  const queryClient = useQueryClient();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['my-lost-reports'],
    queryFn: () => getMyLostReports(1, 20),
  });

  const closeMutation = useMutation({
    mutationFn: (id: string) => closeLostReport(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['my-lost-reports'] });
      void queryClient.invalidateQueries({ queryKey: ['public-board'] });
    },
  });

  return (
    <section aria-labelledby="my-reports-heading">
      <div className={styles.header}>
        <div>
          <h1 id="my-reports-heading">My lost reports</h1>
          <p className="field-hint">
            Only you and authorized campus staff can see your encrypted private details.
          </p>
        </div>
        <Link className="btn btn-primary" to="/report-lost">
          New report
        </Link>
      </div>

      {isLoading ? (
        <p role="status">Loading your reports...</p>
      ) : isError ? (
        <p className="alert-error" role="alert">
          {error instanceof Error ? error.message : 'Unable to load your reports.'}
        </p>
      ) : !data || data.items.length === 0 ? (
        <p className="alert-info">You have not filed any lost reports yet.</p>
      ) : (
        <div className={styles.list}>
          {data.items.map((report) => (
            <article key={report.id} className={styles.reportCard}>
              <div className={styles.header}>
                <div>
                  <h2>{report.title}</h2>
                  <p className="field-hint">
                    {CATEGORY_LABELS[report.category] ?? report.category} &middot;{' '}
                    {report.campusArea} &middot; Lost {report.dateLost} &middot; Status:{' '}
                    <strong>{report.status}</strong>
                  </p>
                </div>
                {report.status !== 'Closed' && report.status !== 'Returned' ? (
                  <button
                    type="button"
                    className="btn btn-secondary"
                    disabled={closeMutation.isPending}
                    onClick={() => closeMutation.mutate(report.id)}
                  >
                    Close report
                  </button>
                ) : null}
              </div>

              {report.photoUrl ? (
                <img
                  className={styles.preview}
                  src={resolveApiUrl(report.photoUrl)}
                  alt={`Sanitized photo of ${report.title}`}
                />
              ) : null}

              {report.publicDescription ? <p>{report.publicDescription}</p> : null}

              <div className={styles.privateBox}>
                <strong>Private details (owner-only):</strong>
                <p>Identifying notes: {report.privateDetails || 'None'}</p>
                <p>
                  Contact: {report.contactPhone} &middot; {report.contactEmail}
                </p>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
