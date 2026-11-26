import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { CATEGORY_LABELS } from '../types/common';
import type { PublicReport } from '../types/reports';
import { resolveReportPhoto } from '../utils/categoryPhotos';
import styles from './ItemDetailModal.module.css';

export interface ItemDetailPrivateInfo {
  privateDetails: string;
  contactPhone: string;
  contactEmail: string;
}

interface ItemDetailModalProps {
  report: PublicReport | null;
  privateInfo?: ItemDetailPrivateInfo | null;
  canDelete?: boolean;
  isDeleting?: boolean;
  onDelete?: (reportId: string) => Promise<void> | void;
  onClose: () => void;
}

export function ItemDetailModal({
  report,
  privateInfo = null,
  canDelete = false,
  isDeleting = false,
  onDelete,
  onClose,
}: ItemDetailModalProps) {
  const closeButtonRef = useRef<HTMLButtonElement | null>(null);
  const [failedPhotoUrl, setFailedPhotoUrl] = useState<string | null>(null);
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  useEffect(() => {
    setConfirmingDelete(false);
    setDeleteError(null);
  }, [report?.id]);

  useEffect(() => {
    if (!report) {
      return;
    }

    closeButtonRef.current?.focus();

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [report, onClose]);

  if (!report) {
    return null;
  }

  const categoryLabel = CATEGORY_LABELS[report.category] ?? report.category;
  const imageFailed = Boolean(report.photoUrl && failedPhotoUrl === report.photoUrl);
  const photo = resolveReportPhoto(report.category, report.title, report.photoUrl, imageFailed);
  const isFound = Boolean(report.heldAt);
  const postedLabel = Number.isNaN(Date.parse(report.postedAt))
    ? report.postedAt
    : new Date(report.postedAt).toLocaleString();

  const handleConfirmDelete = async () => {
    if (!onDelete) {
      return;
    }

    setDeleteError(null);
    try {
      await onDelete(report.id);
      onClose();
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : 'Unable to delete report right now.');
    }
  };

  return (
    <div
      className={styles.backdrop}
      role="presentation"
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onClose();
        }
      }}
    >
      <div
        className={styles.dialog}
        role="dialog"
        aria-modal="true"
        aria-labelledby="item-detail-title"
      >
        <div className={styles.topBar}>
          <div className={styles.titleGroup}>
            <h2 id="item-detail-title" className={styles.title}>
              {report.title}
            </h2>
            <span className={styles.badge}>{report.status}</span>
          </div>
          <button
            ref={closeButtonRef}
            type="button"
            className={styles.closeIconBtn}
            aria-label="Close item details"
            onClick={onClose}
          >
            &times;
          </button>
        </div>

        <div className={styles.mediaStage}>
          <img
            className={styles.media}
            src={photo.src}
            alt={photo.alt}
            onError={() => {
              if (report.photoUrl) {
                setFailedPhotoUrl(report.photoUrl);
              }
            }}
          />
          <span className={styles.photoTag}>
            {photo.isFallback ? `${categoryLabel} category preview` : 'Sanitized uploaded photo'}
          </span>
        </div>

        <div className={styles.content}>
          <dl className={styles.detailsGrid}>
            <div className={styles.detailItem}>
              <dt>Category</dt>
              <dd>{categoryLabel}</dd>
            </div>
            <div className={styles.detailItem}>
              <dt>Campus area</dt>
              <dd>{report.campusArea}</dd>
            </div>
            <div className={styles.detailItem}>
              <dt>{isFound ? 'Date found' : 'Date lost'}</dt>
              <dd>{report.dateLost}</dd>
            </div>
            {report.heldAt ? (
              <div className={styles.detailItem}>
                <dt>Held at</dt>
                <dd>{report.heldAt}</dd>
              </div>
            ) : null}
            <div className={styles.detailItem}>
              <dt>Posted</dt>
              <dd>{postedLabel}</dd>
            </div>
          </dl>

          <div className={styles.descriptionBlock}>
            <h3>Public description</h3>
            <p>{report.publicDescription || 'No additional public description provided.'}</p>
          </div>

          {privateInfo ? (
            <div className={styles.privateBox}>
              <strong>Private details (owner-only)</strong>
              <p>Identifying notes: {privateInfo.privateDetails || 'None'}</p>
              <p>
                Contact: {privateInfo.contactPhone} &middot; {privateInfo.contactEmail}
              </p>
            </div>
          ) : (
            <p className={styles.privacyNote}>
              Private contact details and identifying marks stay encrypted and are never exposed on
              the public board.
            </p>
          )}

          {deleteError ? (
            <p className="alert-error" role="alert">
              {deleteError}
            </p>
          ) : null}
        </div>

        <div className={styles.actions}>
          {canDelete && onDelete ? (
            confirmingDelete ? (
              <>
                <span className="field-hint" style={{ marginRight: 'auto', alignSelf: 'center' }}>
                  Found it yourself? Remove this report permanently?
                </span>
                <button
                  type="button"
                  className="btn btn-danger"
                  disabled={isDeleting}
                  onClick={() => void handleConfirmDelete()}
                >
                  {isDeleting ? 'Deleting...' : 'Confirm delete'}
                </button>
                <button
                  type="button"
                  className="btn btn-secondary"
                  disabled={isDeleting}
                  onClick={() => setConfirmingDelete(false)}
                >
                  Cancel
                </button>
              </>
            ) : (
              <button
                type="button"
                className="btn btn-danger"
                onClick={() => setConfirmingDelete(true)}
              >
                Found it myself (Delete)
              </button>
            )
          ) : null}
          {isFound && (report.status === 'Open' || report.status === 'Matched') ? (
            <Link className="btn btn-primary" to={`/claim/${report.id}`} onClick={onClose}>
              Claim this item
            </Link>
          ) : !isFound && report.status === 'Open' ? (
            <Link className="btn btn-primary" to="/report-found" onClick={onClose}>
              I found something similar
            </Link>
          ) : null}
          <button type="button" className="btn btn-secondary" onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
