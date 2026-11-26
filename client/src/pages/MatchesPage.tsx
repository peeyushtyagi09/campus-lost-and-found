import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { confirmMatch, dismissMatch, getMyMatches, runMatching } from '../api/matches';
import { ItemCard } from '../components/ItemCard';
import { ItemDetailModal } from '../components/ItemDetailModal';
import type { PublicReport } from '../types/reports';
import styles from './BoardPage.module.css';

export function MatchesPage() {
  const queryClient = useQueryClient();
  const [selectedReport, setSelectedReport] = useState<PublicReport | null>(null);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['my-matches'],
    queryFn: getMyMatches,
  });

  const recomputeMutation = useMutation({
    mutationFn: runMatching,
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['my-matches'] }),
  });

  const confirmMutation = useMutation({
    mutationFn: (id: string) => confirmMatch(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['my-matches'] }),
  });

  const dismissMutation = useMutation({
    mutationFn: (id: string) => dismissMatch(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['my-matches'] }),
  });

  return (
    <section aria-labelledby="matches-heading">
      <div className={styles.header}>
        <div>
          <h1 id="matches-heading">Possible matches</h1>
          <p className="field-hint">
            Suggestions show public details of found items only. Confirm a match to start a claim.
          </p>
        </div>
        <button
          type="button"
          className="btn btn-secondary"
          disabled={recomputeMutation.isPending}
          onClick={() => recomputeMutation.mutate()}
        >
          Refresh matches
        </button>
      </div>

      {isLoading ? (
        <p role="status">Loading match suggestions...</p>
      ) : isError ? (
        <p className="alert-error" role="alert">
          {error instanceof Error ? error.message : 'Unable to load matches.'}
        </p>
      ) : !data || data.length === 0 ? (
        <p className="alert-info">No match suggestions yet.</p>
      ) : (
        <div className={styles.list}>
          {data.map((match) => (
            <article key={match.id} className={styles.reportCard}>
              <div className={styles.header}>
                <div>
                  <h2>
                    Score: {match.score}/100 ({match.status})
                  </h2>
                  <p className="field-hint">{match.explanation}</p>
                </div>
                <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                  {match.status === 'Suggested' ? (
                    <>
                      <button
                        type="button"
                        className="btn btn-primary"
                        disabled={confirmMutation.isPending}
                        onClick={() => confirmMutation.mutate(match.id)}
                      >
                        Confirm match
                      </button>
                      <button
                        type="button"
                        className="btn btn-secondary"
                        disabled={dismissMutation.isPending}
                        onClick={() => dismissMutation.mutate(match.id)}
                      >
                        Dismiss
                      </button>
                    </>
                  ) : match.status === 'Confirmed' ? (
                    <Link
                      className="btn btn-primary"
                      to={`/claim/${match.foundReportId}?lostReportId=${match.lostReportId}`}
                    >
                      Claim this item
                    </Link>
                  ) : null}
                </div>
              </div>
              <ItemCard report={match.foundItem} onOpen={setSelectedReport} />
            </article>
          ))}
        </div>
      )}
      <ItemDetailModal report={selectedReport} onClose={() => setSelectedReport(null)} />
    </section>
  );
}
