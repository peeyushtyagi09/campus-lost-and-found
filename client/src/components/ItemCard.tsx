import { Link } from 'react-router-dom';
import { resolveApiUrl } from '../api/http';
import { CATEGORY_LABELS } from '../types/common';
import type { PublicReport } from '../types/reports';
import styles from './ItemCard.module.css';

interface ItemCardProps {
  report: PublicReport;
  showClaimLink?: boolean;
}

export function ItemCard({ report, showClaimLink = false }: ItemCardProps) {
  const categoryLabel = CATEGORY_LABELS[report.category] ?? report.category;

  return (
    <article className={styles.card} aria-label={report.title}>
      {report.photoUrl ? (
        <img
          className={styles.media}
          src={resolveApiUrl(report.photoUrl)}
          alt={`Sanitized photo of ${report.title}`}
          loading="lazy"
        />
      ) : (
        <div className={styles.placeholder} aria-hidden="true">
          No photo provided
        </div>
      )}
      <div className={styles.body}>
        <div className={styles.headerRow}>
          <h2 className={styles.title}>{report.title}</h2>
          <span className={styles.badge}>{report.status}</span>
        </div>
        <p className={styles.meta}>
          {categoryLabel} &middot; {report.campusArea} &middot;{' '}
          {report.heldAt ? `Found ${report.dateLost}` : `Lost on ${report.dateLost}`}
        </p>
        {report.heldAt ? <p className={styles.meta}>Held at: {report.heldAt}</p> : null}
        {report.publicDescription ? (
          <p className={styles.description}>{report.publicDescription}</p>
        ) : null}
        {showClaimLink &&
        report.heldAt &&
        (report.status === 'Open' || report.status === 'Matched') ? (
          <Link className="btn btn-secondary" to={`/claim/${report.id}`}>
            Claim this item
          </Link>
        ) : null}
      </div>
    </article>
  );
}
