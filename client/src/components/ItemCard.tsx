import { useState } from 'react';
import { Link } from 'react-router-dom';
import { CATEGORY_LABELS } from '../types/common';
import type { PublicReport } from '../types/reports';
import { resolveReportPhoto } from '../utils/categoryPhotos';
import { ItemDetailModal } from './ItemDetailModal';
import styles from './ItemCard.module.css';

interface ItemCardProps {
  report: PublicReport;
  showClaimLink?: boolean;
  onOpen?: (report: PublicReport) => void;
}

export function ItemCard({ report, showClaimLink = false, onOpen }: ItemCardProps) {
  const [internalOpen, setInternalOpen] = useState(false);
  const [failedPhotoUrl, setFailedPhotoUrl] = useState<string | null>(null);

  const categoryLabel = CATEGORY_LABELS[report.category] ?? report.category;
  const imageFailed = Boolean(report.photoUrl && failedPhotoUrl === report.photoUrl);
  const photo = resolveReportPhoto(report.category, report.title, report.photoUrl, imageFailed);

  const handleOpen = () => {
    if (onOpen) {
      onOpen(report);
      return;
    }

    setInternalOpen(true);
  };

  return (
    <>
      <article className={styles.card} aria-label={report.title}>
        <div className={styles.mediaWrap}>
          <img
            className={styles.media}
            src={photo.src}
            alt={photo.alt}
            loading="lazy"
            onError={() => {
              if (report.photoUrl) {
                setFailedPhotoUrl(report.photoUrl);
              }
            }}
          />
          {photo.isFallback ? (
            <span className={styles.fallbackTag} aria-hidden="true">
              {categoryLabel} preview
            </span>
          ) : null}
        </div>
        <div className={styles.body}>
          <div className={styles.headerRow}>
            <h2 className={styles.title}>
              <button type="button" className={styles.cardTrigger} onClick={handleOpen}>
                {report.title}
              </button>
            </h2>
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
          <div className={styles.footerRow}>
            <span className={styles.openHint} aria-hidden="true">
              Open details &rarr;
            </span>
            {showClaimLink &&
            report.heldAt &&
            (report.status === 'Open' || report.status === 'Matched') ? (
              <div className={styles.actions}>
                <Link className="btn btn-secondary" to={`/claim/${report.id}`}>
                  Claim this item
                </Link>
              </div>
            ) : null}
          </div>
        </div>
      </article>
      {!onOpen && internalOpen ? (
        <ItemDetailModal report={report} onClose={() => setInternalOpen(false)} />
      ) : null}
    </>
  );
}
