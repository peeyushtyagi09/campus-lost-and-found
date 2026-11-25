import { useQuery } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { getCustodyTimeline } from '../api/custody';
import { CustodyTimeline } from '../components/CustodyTimeline';

export function CustodyPage() {
  const { reportId = '' } = useParams();

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['custody-timeline', reportId],
    queryFn: () => getCustodyTimeline(reportId),
    enabled: Boolean(reportId),
  });

  return (
    <section aria-labelledby="custody-heading">
      <h1 id="custody-heading">Custody record</h1>
      {isLoading ? (
        <p role="status">Loading custody timeline...</p>
      ) : isError ? (
        <p className="alert-error" role="alert">
          {error instanceof Error ? error.message : 'Unable to load custody record.'}
        </p>
      ) : data ? (
        <CustodyTimeline timeline={data} />
      ) : null}
    </section>
  );
}
