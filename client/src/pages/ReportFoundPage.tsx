import { useEffect, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { createFoundReport } from '../api/foundReports';
import { ApiError } from '../api/http';
import { uploadPhoto } from '../api/uploads';
import { CAMPUS_AREAS, CATEGORY_LABELS, ITEM_CATEGORIES, type ItemCategory } from '../types/common';
import { containsContactInfo, validatePhotoFile } from '../utils/validation';
import styles from './BoardPage.module.css';

const HELD_DESKS = ['Main Security Desk', 'Library Front Desk', 'Student Union Security'] as const;

const reportFoundSchema = z.object({
  title: z
    .string()
    .trim()
    .min(3, 'Title must be at least 3 characters.')
    .max(100, 'Title must be at most 100 characters.')
    .refine((val) => !containsContactInfo(val), 'Title must not contain contact info.'),
  category: z.enum(ITEM_CATEGORIES as [ItemCategory, ...ItemCategory[]]),
  campusArea: z.string().trim().min(2, 'Campus area is required.').max(80),
  heldAt: z
    .string()
    .trim()
    .min(2, 'Security desk name is required.')
    .max(80)
    .refine((val) => !containsContactInfo(val), 'HeldAt must not contain contact info.'),
  dateFound: z
    .string()
    .min(1, 'Date found is required.')
    .refine((val) => {
      const parsed = new Date(val);
      return !Number.isNaN(parsed.getTime()) && parsed.getTime() <= Date.now() + 86_400_000;
    }, 'Date found cannot be in the future.'),
  publicDescription: z
    .string()
    .trim()
    .max(500)
    .refine(
      (val) => !containsContactInfo(val),
      'Public description must not contain contact info.'
    ),
  storageNotes: z.string().trim().max(500, 'Storage notes must be at most 500 characters.'),
  finderContact: z.string().trim().min(3, 'Finder contact is required.').max(120),
  verificationDetails: z.string().trim().min(3, 'Verification details are required.').max(1000),
});

type ReportFoundFormValues = z.infer<typeof reportFoundSchema>;

export function ReportFoundPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [photoError, setPhotoError] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ReportFoundFormValues>({
    resolver: zodResolver(reportFoundSchema),
    defaultValues: {
      category: 'Phone',
      campusArea: CAMPUS_AREAS[0] ?? 'Library',
      heldAt: HELD_DESKS[0],
      dateFound: new Date().toISOString().slice(0, 10),
      publicDescription: '',
      storageNotes: '',
    },
  });

  useEffect(() => {
    if (!selectedFile || typeof URL.createObjectURL !== 'function') {
      setPreviewUrl(null);
      return;
    }
    const url = URL.createObjectURL(selectedFile);
    setPreviewUrl(url);
    return () => {
      if (typeof URL.revokeObjectURL === 'function') {
        URL.revokeObjectURL(url);
      }
    };
  }, [selectedFile]);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null;
    if (!file) {
      setSelectedFile(null);
      setPhotoError(null);
      return;
    }
    const validationError = validatePhotoFile(file);
    if (validationError) {
      setSelectedFile(null);
      setPhotoError(validationError);
      return;
    }
    setPhotoError(null);
    setSelectedFile(file);
  };

  const onSubmit = async (values: ReportFoundFormValues) => {
    setServerError(null);
    if (photoError) {
      return;
    }
    try {
      let photoFileName: string | null = null;
      if (selectedFile) {
        const uploaded = await uploadPhoto(selectedFile);
        photoFileName = uploaded.fileName;
      }
      await createFoundReport({ ...values, photoFileName });
      await queryClient.invalidateQueries({ queryKey: ['public-board'] });
      await queryClient.invalidateQueries({ queryKey: ['my-found-reports'] });
      await queryClient.invalidateQueries({ queryKey: ['my-matches'] });
      navigate('/?type=found', {
        state: { notice: 'Found item reported! Showing Found items on the board.' },
      });
    } catch (err) {
      setServerError(err instanceof ApiError ? err.message : 'Unable to submit found report.');
    }
  };

  return (
    <section aria-labelledby="report-found-heading">
      <h1 id="report-found-heading">Report a found item</h1>
      {serverError ? (
        <p className="alert-error" role="alert">
          {serverError}
        </p>
      ) : null}

      <form className="form-card" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="field-group">
          <label className="field-label" htmlFor="found-title">
            Item title (public)
          </label>
          <input id="found-title" type="text" className="field-input" {...register('title')} />
          {errors.title ? (
            <p className="field-error" role="alert">
              {errors.title.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="found-category">
            Category (public)
          </label>
          <select id="found-category" className="field-select" {...register('category')}>
            {ITEM_CATEGORIES.map((item) => (
              <option key={item} value={item}>
                {CATEGORY_LABELS[item]}
              </option>
            ))}
          </select>
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="found-area">
            Area found (public)
          </label>
          <select id="found-area" className="field-select" {...register('campusArea')}>
            {CAMPUS_AREAS.map((area) => (
              <option key={area} value={area}>
                {area}
              </option>
            ))}
          </select>
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="found-held-at">
            Security desk holding item (public)
          </label>
          <select id="found-held-at" className="field-select" {...register('heldAt')}>
            {HELD_DESKS.map((desk) => (
              <option key={desk} value={desk}>
                {desk}
              </option>
            ))}
          </select>
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="found-date">
            Date found (public)
          </label>
          <input id="found-date" type="date" className="field-input" {...register('dateFound')} />
          {errors.dateFound ? (
            <p className="field-error" role="alert">
              {errors.dateFound.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="found-public-desc">
            Public description
          </label>
          <textarea
            id="found-public-desc"
            rows={3}
            className="field-textarea"
            {...register('publicDescription')}
          />
          {errors.publicDescription ? (
            <p className="field-error" role="alert">
              {errors.publicDescription.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="found-photo">
            Optional photo (JPEG, PNG, or WebP, max 5 MB)
          </label>
          <input
            id="found-photo"
            type="file"
            accept="image/jpeg,image/png,image/webp"
            onChange={handleFileChange}
          />
          <p className="field-hint">Photo location data is removed automatically.</p>
          {photoError ? (
            <p className="field-error" role="alert">
              {photoError}
            </p>
          ) : null}
          {previewUrl ? (
            <img className={styles.preview} src={previewUrl} alt="Selected found item preview" />
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="found-storage-notes">
            Locker / shelf storage notes (encrypted, staff-only)
          </label>
          <input
            id="found-storage-notes"
            type="text"
            className="field-input"
            {...register('storageNotes')}
          />
          {errors.storageNotes ? (
            <p className="field-error" role="alert">
              {errors.storageNotes.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="found-finder-contact">
            Finder contact (encrypted, private)
          </label>
          <input
            id="found-finder-contact"
            type="text"
            className="field-input"
            {...register('finderContact')}
          />
          {errors.finderContact ? (
            <p className="field-error" role="alert">
              {errors.finderContact.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="found-verification-details">
            Verification details (encrypted, used to verify owner claims)
          </label>
          <textarea
            id="found-verification-details"
            rows={3}
            className="field-textarea"
            {...register('verificationDetails')}
          />
          {errors.verificationDetails ? (
            <p className="field-error" role="alert">
              {errors.verificationDetails.message}
            </p>
          ) : null}
        </div>

        <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Submitting...' : 'Submit found report'}
        </button>
      </form>
    </section>
  );
}
