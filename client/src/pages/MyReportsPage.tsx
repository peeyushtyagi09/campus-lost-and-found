import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { closeFoundReport, getMyFoundReports } from '../api/foundReports';
import { closeLostReport, deleteLostReport, getMyLostReports } from '../api/lostReports';
import { ItemDetailModal, type ItemDetailPrivateInfo } from '../components/ItemDetailModal';
import { CATEGORY_LABELS } from '../types/common';
import type { FoundOwnerReport, OwnerReport, PublicReport } from '../types/reports';
import { resolveReportPhoto } from '../utils/categoryPhotos';
import styles from './BoardPage.module.css';

function toPublicLostSummary(report: OwnerReport): PublicReport {
  return {
    id: report.id,
    title: report.title,
    category: report.category,
    publicDescription: report.publicDescription,
    campusArea: report.campusArea,
    dateLost: report.dateLost,
    photoUrl: report.photoUrl,
    status: report.status,
    postedAt: report.createdAt,
  };
}

function toPublicFoundSummary(report: FoundOwnerReport): PublicReport {
  return {
    id: report.id,
    title: report.title,
    category: report.category,
    publicDescription: report.publicDescription,
    campusArea: report.campusArea,
    dateLost: report.dateFound,
    photoUrl: report.photoUrl,
    status: report.status,
    postedAt: report.createdAt,
    heldAt: report.heldAt,
  };
}

interface SelectedModalState {
  publicSummary: PublicReport;
  privateInfo: ItemDetailPrivateInfo | null;
  canDelete: boolean;
}

export function MyReportsPage() {
  const queryClient = useQueryClient();
  const [selectedModal, setSelectedModal] = useState<SelectedModalState | null>(null);
  const [confirmDeleteId, setConfirmDeleteId] = useState<string | null>(null);
  const [failedPhotoIds, setFailedPhotoIds] = useState<Record<string, boolean>>({});

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['my-lost-reports'],
    queryFn: () => getMyLostReports(1, 20),
  });

  const foundReportsQuery = useQuery({
    queryKey: ['my-found-reports'],
    queryFn: () => getMyFoundReports(1, 20),
  });

  const closeMutation = useMutation({
    mutationFn: (id: string) => closeLostReport(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['my-lost-reports'] });
      void queryClient.invalidateQueries({ queryKey: ['public-board'] });
    },
  });

  const closeFoundMutation = useMutation({
    mutationFn: (id: string) => closeFoundReport(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['my-found-reports'] });
      void queryClient.invalidateQueries({ queryKey: ['public-board'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteLostReport(id),
    onSuccess: (_data, deletedId) => {
      setConfirmDeleteId(null);
      if (selectedModal?.publicSummary.id === deletedId) {
        setSelectedModal(null);
      }
      void queryClient.invalidateQueries({ queryKey: ['my-lost-reports'] });
      void queryClient.invalidateQueries({ queryKey: ['public-board'] });
      void queryClient.invalidateQueries({ queryKey: ['my-matches'] });
    },
  });

  const openLostReport = (report: OwnerReport) => {
    setSelectedModal({
      publicSummary: toPublicLostSummary(report),
      privateInfo: {
        privateDetails: report.privateDetails,
        contactPhone: report.contactPhone,
        contactEmail: report.contactEmail,
      },
      canDelete: report.status !== 'Claimed' && report.status !== 'Returned',
    });
  };

  const openFoundReport = (report: FoundOwnerReport) => {
    setSelectedModal({
      publicSummary: toPublicFoundSummary(report),
      privateInfo: {
        privateDetails: `${report.verificationDetails}${report.storageNotes ? ` (Storage: ${report.storageNotes})` : ''}`,
        contactPhone: report.finderContact,
        contactEmail: report.heldAt,
      },
      canDelete: false,
    });
  };

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

      {deleteMutation.isError ? (
        <p className="alert-error" role="alert">
          {deleteMutation.error instanceof Error
            ? deleteMutation.error.message
            : 'Unable to delete lost report.'}
        </p>
      ) : null}

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
          {data.items.map((report) => {
            const photo = resolveReportPhoto(
              report.category,
              report.title,
              report.photoUrl,
              Boolean(failedPhotoIds[report.id])
            );
            const canDelete = report.status !== 'Claimed' && report.status !== 'Returned';

            return (
              <article key={report.id} className={styles.reportCard}>
                <div className={styles.header}>
                  <div>
                    <h2>
                      <button
                        type="button"
                        style={{
                          appearance: 'none',
                          background: 'none',
                          border: 0,
                          padding: 0,
                          font: 'inherit',
                          color: 'inherit',
                          cursor: 'pointer',
                          textAlign: 'left',
                          textDecoration: 'underline',
                          textUnderlineOffset: '3px',
                        }}
                        onClick={() => openLostReport(report)}
                      >
                        {report.title}
                      </button>
                    </h2>
                    <p className="field-hint">
                      {CATEGORY_LABELS[report.category] ?? report.category} &middot;{' '}
                      {report.campusArea} &middot; Lost {report.dateLost} &middot; Status:{' '}
                      <strong>{report.status}</strong>
                    </p>
                  </div>
                  <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={() => openLostReport(report)}
                    >
                      Open card
                    </button>
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
                    {canDelete ? (
                      confirmDeleteId === report.id ? (
                        <>
                          <button
                            type="button"
                            className="btn btn-danger"
                            disabled={deleteMutation.isPending}
                            onClick={() => deleteMutation.mutate(report.id)}
                          >
                            {deleteMutation.isPending ? 'Deleting...' : 'Confirm delete'}
                          </button>
                          <button
                            type="button"
                            className="btn btn-secondary"
                            disabled={deleteMutation.isPending}
                            onClick={() => setConfirmDeleteId(null)}
                          >
                            Cancel
                          </button>
                        </>
                      ) : (
                        <button
                          type="button"
                          className="btn btn-danger"
                          onClick={() => setConfirmDeleteId(report.id)}
                        >
                          Found it myself (Delete)
                        </button>
                      )
                    ) : null}
                  </div>
                </div>

                <img
                  className={styles.preview}
                  src={photo.src}
                  alt={photo.alt}
                  onError={() => setFailedPhotoIds((prev) => ({ ...prev, [report.id]: true }))}
                />

                {report.publicDescription ? <p>{report.publicDescription}</p> : null}

                <div className={styles.privateBox}>
                  <strong>Private details (owner-only):</strong>
                  <p>Identifying notes: {report.privateDetails || 'None'}</p>
                  <p>
                    Contact: {report.contactPhone} &middot; {report.contactEmail}
                  </p>
                </div>
              </article>
            );
          })}
        </div>
      )}

      <div className={styles.header} style={{ marginTop: '2.5rem' }}>
        <div>
          <h2 id="my-found-reports-heading">My found reports</h2>
          <p className="field-hint">Items you have turned in and logged on the campus board.</p>
        </div>
        <Link className="btn btn-secondary" to="/report-found">
          Report found item
        </Link>
      </div>

      {foundReportsQuery.isLoading ? (
        <p role="status">Loading your found reports...</p>
      ) : foundReportsQuery.isError ? (
        <p className="alert-error" role="alert">
          {foundReportsQuery.error instanceof Error
            ? foundReportsQuery.error.message
            : 'Unable to load your found reports.'}
        </p>
      ) : !foundReportsQuery.data || foundReportsQuery.data.items.length === 0 ? (
        <p className="alert-info">You have not filed any found reports yet.</p>
      ) : (
        <div className={styles.list}>
          {foundReportsQuery.data.items.map((report) => {
            const photo = resolveReportPhoto(
              report.category,
              report.title,
              report.photoUrl,
              Boolean(failedPhotoIds[report.id])
            );

            return (
              <article key={report.id} className={styles.reportCard}>
                <div className={styles.header}>
                  <div>
                    <h3>
                      <button
                        type="button"
                        style={{
                          appearance: 'none',
                          background: 'none',
                          border: 0,
                          padding: 0,
                          font: 'inherit',
                          color: 'inherit',
                          cursor: 'pointer',
                          textAlign: 'left',
                          textDecoration: 'underline',
                          textUnderlineOffset: '3px',
                        }}
                        onClick={() => openFoundReport(report)}
                      >
                        {report.title}
                      </button>
                    </h3>
                    <p className="field-hint">
                      {CATEGORY_LABELS[report.category] ?? report.category} &middot;{' '}
                      {report.campusArea} &middot; Found {report.dateFound} &middot; Held at{' '}
                      {report.heldAt} &middot; Status: <strong>{report.status}</strong>
                    </p>
                  </div>
                  <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={() => openFoundReport(report)}
                    >
                      Open card
                    </button>
                    {report.status !== 'Closed' && report.status !== 'Returned' ? (
                      <button
                        type="button"
                        className="btn btn-secondary"
                        disabled={closeFoundMutation.isPending}
                        onClick={() => closeFoundMutation.mutate(report.id)}
                      >
                        Close found report
                      </button>
                    ) : null}
                  </div>
                </div>

                <img
                  className={styles.preview}
                  src={photo.src}
                  alt={photo.alt}
                  onError={() => setFailedPhotoIds((prev) => ({ ...prev, [report.id]: true }))}
                />

                {report.publicDescription ? <p>{report.publicDescription}</p> : null}

                <div className={styles.privateBox}>
                  <strong>Private details (finder/staff):</strong>
                  <p>Verification details: {report.verificationDetails || 'None'}</p>
                  {report.storageNotes ? <p>Storage notes: {report.storageNotes}</p> : null}
                  <p>Finder contact: {report.finderContact}</p>
                </div>
              </article>
            );
          })}
        </div>
      )}

      <ItemDetailModal
        report={selectedModal ? selectedModal.publicSummary : null}
        privateInfo={selectedModal ? selectedModal.privateInfo : null}
        canDelete={Boolean(selectedModal?.canDelete)}
        isDeleting={deleteMutation.isPending}
        onDelete={async (id) => {
          await deleteMutation.mutateAsync(id);
        }}
        onClose={() => setSelectedModal(null)}
      />
    </section>
  );
}
