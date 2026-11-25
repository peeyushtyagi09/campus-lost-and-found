import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { approveClaim, completeHandover, getReviewQueue, rejectClaim } from '../api/claims';
import { ApiError } from '../api/http';
import styles from './BoardPage.module.css';

export function StaffDashboardPage() {
  const queryClient = useQueryClient();
  const [reviewNotes, setReviewNotes] = useState<Record<string, string>>({});
  const [witnessChecked, setWitnessChecked] = useState<Record<string, boolean>>({});
  const [idChecked, setIdChecked] = useState<Record<string, boolean>>({});
  const [actionError, setActionError] = useState<string | null>(null);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['staff-claims'],
    queryFn: () => getReviewQueue(),
  });

  const refresh = () => void queryClient.invalidateQueries({ queryKey: ['staff-claims'] });

  const approveMutation = useMutation({
    mutationFn: ({ id, notes }: { id: string; notes: string }) => approveClaim(id, notes),
    onSuccess: refresh,
    onError: (err) => setActionError(err instanceof ApiError ? err.message : 'Approval failed.'),
  });

  const rejectMutation = useMutation({
    mutationFn: ({ id, notes }: { id: string; notes: string }) => rejectClaim(id, notes),
    onSuccess: refresh,
    onError: (err) => setActionError(err instanceof ApiError ? err.message : 'Rejection failed.'),
  });

  const handoverMutation = useMutation({
    mutationFn: ({ id, witness, idOk }: { id: string; witness: boolean; idOk: boolean }) =>
      completeHandover(id, { witnessConfirmed: witness, idChecked: idOk }),
    onSuccess: refresh,
    onError: (err) => setActionError(err instanceof ApiError ? err.message : 'Handover failed.'),
  });

  return (
    <section aria-labelledby="staff-heading">
      <h1 id="staff-heading">Staff claim &amp; handover queue</h1>
      {actionError ? (
        <p className="alert-error" role="alert">
          {actionError}
        </p>
      ) : null}

      {isLoading ? (
        <p role="status">Loading claim queue...</p>
      ) : isError ? (
        <p className="alert-error" role="alert">
          {error instanceof Error ? error.message : 'Unable to load staff queue.'}
        </p>
      ) : !data || data.length === 0 ? (
        <p className="alert-info">No claims in queue.</p>
      ) : (
        <div className={styles.list}>
          {data.map((claim) => (
            <article key={claim.id} className={styles.reportCard}>
              <div className={styles.header}>
                <div>
                  <h2>{claim.foundItem?.title ?? `Found item ${claim.foundReportId}`}</h2>
                  <p className="field-hint">
                    Claim status: <strong>{claim.status}</strong> &middot; Item status:{' '}
                    <strong>{claim.foundItem?.status ?? 'Unknown'}</strong>
                  </p>
                </div>
                <Link className="btn btn-secondary" to={`/custody/${claim.foundReportId}`}>
                  Custody timeline
                </Link>
              </div>

              <div className={styles.privateBox}>
                <p>
                  <strong>Claimant proof:</strong> {claim.verificationAnswers}
                </p>
                <p>
                  <strong>Finder verification details:</strong> {claim.foundVerificationDetails}
                </p>
                <p>
                  <strong>Storage location:</strong> {claim.foundStorageNotes}
                </p>
              </div>

              {claim.status === 'Pending' ? (
                <div style={{ marginTop: '0.75rem' }}>
                  <div className="field-group">
                    <label className="field-label" htmlFor={`notes-${claim.id}`}>
                      Staff review notes
                    </label>
                    <input
                      id={`notes-${claim.id}`}
                      type="text"
                      className="field-input"
                      value={reviewNotes[claim.id] ?? ''}
                      onChange={(e) =>
                        setReviewNotes((prev) => ({ ...prev, [claim.id]: e.target.value }))
                      }
                    />
                  </div>
                  <div style={{ display: 'flex', gap: '0.5rem' }}>
                    <button
                      type="button"
                      className="btn btn-primary"
                      onClick={() =>
                        approveMutation.mutate({
                          id: claim.id,
                          notes: reviewNotes[claim.id] ?? '',
                        })
                      }
                    >
                      Approve claim
                    </button>
                    <button
                      type="button"
                      className="btn btn-danger"
                      onClick={() =>
                        rejectMutation.mutate({
                          id: claim.id,
                          notes: reviewNotes[claim.id] ?? '',
                        })
                      }
                    >
                      Reject claim
                    </button>
                  </div>
                </div>
              ) : null}

              {claim.status === 'Approved' && claim.foundItem?.status !== 'Returned' ? (
                <div style={{ marginTop: '0.75rem' }}>
                  <div className="field-group">
                    <label>
                      <input
                        type="checkbox"
                        checked={Boolean(idChecked[claim.id])}
                        onChange={(e) =>
                          setIdChecked((prev) => ({ ...prev, [claim.id]: e.target.checked }))
                        }
                      />{' '}
                      Claimant campus/government ID verified
                    </label>
                    <label>
                      <input
                        type="checkbox"
                        checked={Boolean(witnessChecked[claim.id])}
                        onChange={(e) =>
                          setWitnessChecked((prev) => ({ ...prev, [claim.id]: e.target.checked }))
                        }
                      />{' '}
                      Supervised handover witness confirmed
                    </label>
                  </div>
                  <button
                    type="button"
                    className="btn btn-primary"
                    disabled={!idChecked[claim.id] || !witnessChecked[claim.id]}
                    onClick={() =>
                      handoverMutation.mutate({
                        id: claim.id,
                        witness: Boolean(witnessChecked[claim.id]),
                        idOk: Boolean(idChecked[claim.id]),
                      })
                    }
                  >
                    Complete handover
                  </button>
                </div>
              ) : null}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
