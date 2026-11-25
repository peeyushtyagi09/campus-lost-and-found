import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { getMyClaims, withdrawClaim } from '../api/claims';
import styles from './BoardPage.module.css';

export function MyClaimsPage() {
  const queryClient = useQueryClient();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['my-claims'],
    queryFn: getMyClaims,
  });

  const withdrawMutation = useMutation({
    mutationFn: (claimId: string) => withdrawClaim(claimId),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['my-claims'] }),
  });

  return (
    <section aria-labelledby="my-claims-heading">
      <h1 id="my-claims-heading">My claims</h1>
      {isLoading ? (
        <p role="status">Loading claims...</p>
      ) : isError ? (
        <p className="alert-error" role="alert">
          {error instanceof Error ? error.message : 'Unable to load claims.'}
        </p>
      ) : !data || data.length === 0 ? (
        <p className="alert-info">You have not submitted any claims yet.</p>
      ) : (
        <div className={styles.list}>
          {data.map((claim) => (
            <article key={claim.id} className={styles.reportCard}>
              <div className={styles.header}>
                <div>
                  <h2>{claim.foundItem?.title ?? `Found item ${claim.foundReportId}`}</h2>
                  <p className="field-hint">
                    Status: <strong>{claim.status}</strong> &middot; Submitted{' '}
                    {new Date(claim.createdAt).toLocaleDateString()}
                  </p>
                </div>
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <Link className="btn btn-secondary" to={`/custody/${claim.foundReportId}`}>
                    Custody record
                  </Link>
                  {claim.status === 'Pending' ? (
                    <button
                      type="button"
                      className="btn btn-secondary"
                      disabled={withdrawMutation.isPending}
                      onClick={() => withdrawMutation.mutate(claim.id)}
                    >
                      Withdraw
                    </button>
                  ) : null}
                </div>
              </div>
              <div className={styles.privateBox}>
                <p>Your verification answer: {claim.verificationAnswers}</p>
                {claim.reviewNotes ? <p>Staff review notes: {claim.reviewNotes}</p> : null}
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
