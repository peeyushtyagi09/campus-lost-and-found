import type { CustodyTimelineDto } from '../types/claims';
import styles from '../pages/BoardPage.module.css';

export function CustodyTimeline({ timeline }: { timeline: CustodyTimelineDto }) {
  return (
    <div className={styles.reportCard}>
      <p className={timeline.isChainValid ? 'alert-success' : 'alert-error'} role="status">
        Custody hash chain: {timeline.isChainValid ? 'Verified intact' : 'Verification failed'}
      </p>
      <ol className={styles.list}>
        {timeline.events.map((evt) => (
          <li key={evt.id} className={styles.privateBox}>
            <strong>
              #{evt.sequenceNumber} {evt.eventType}
            </strong>{' '}
            by <span>{evt.actorRole}</span> &middot;{' '}
            <time dateTime={evt.timestamp}>{new Date(evt.timestamp).toLocaleString()}</time>
            <p>{evt.notes}</p>
            <p className="field-hint">Hash: {evt.eventHash.slice(0, 16)}&hellip;</p>
          </li>
        ))}
      </ol>
    </div>
  );
}
