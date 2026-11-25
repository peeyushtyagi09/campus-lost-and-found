import { useEffect, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { ApiError } from '../api/http';
import { createLostReport } from '../api/lostReports';
import { uploadPhoto } from '../api/uploads';
import { CAMPUS_AREAS, CATEGORY_LABELS, ITEM_CATEGORIES, type ItemCategory } from '../types/common';
import { containsContactInfo, validatePhotoFile } from '../utils/validation';
import styles from './BoardPage.module.css';

const reportLostSchema = z.object({
  title: z
    .string()
    .trim()
    .min(3, 'Title must be at least 3 characters.')
    .max(100, 'Title must be at most 100 characters.')
    .refine((val) => !containsContactInfo(val), 'Title must not contain email or phone numbers.'),
  category: z.enum(ITEM_CATEGORIES as [ItemCategory, ...ItemCategory[]]),
  campusArea: z
    .string()
    .trim()
    .min(2, 'Campus area is required.')
    .max(80, 'Campus area must be at most 80 characters.'),
  dateLost: z
    .string()
    .min(1, 'Date lost is required.')
    .refine((val) => {
      const parsed = new Date(val);
      return !Number.isNaN(parsed.getTime()) && parsed.getTime() <= Date.now() + 86_400_000;
    }, 'Date lost cannot be in the future.'),
  publicDescription: z
    .string()
    .trim()
    .max(500, 'Public description must be at most 500 characters.')
    .refine(
      (val) => !containsContactInfo(val),
      'Public description must not contain email addresses or phone numbers.'
    ),
  privateDetails: z.string().trim().max(1000, 'Private details must be at most 1000 characters.'),
  contactPhone: z
    .string()
    .trim()
    .min(7, 'Contact phone is required.')
    .max(30, 'Contact phone is too long.'),
  contactEmail: z
    .string()
    .trim()
    .min(1, 'Contact email is required.')
    .email('Enter a valid contact email.'),
});

type ReportLostFormValues = z.infer<typeof reportLostSchema>;

export function ReportLostPage() {
  const navigate = useNavigate();
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [photoError, setPhotoError] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ReportLostFormValues>({
    resolver: zodResolver(reportLostSchema),
    defaultValues: {
      category: 'Phone',
      campusArea: CAMPUS_AREAS[0] ?? 'Library',
      dateLost: new Date().toISOString().slice(0, 10),
      publicDescription: '',
      privateDetails: '',
    },
  });

  useEffect(() => {
    if (!selectedFile) {
      setPreviewUrl(null);
      return;
    }

    const url = URL.createObjectURL(selectedFile);
    setPreviewUrl(url);
    return () => {
      URL.revokeObjectURL(url);
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

  const onSubmit = async (values: ReportLostFormValues) => {
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

      await createLostReport({
        ...values,
        photoFileName,
      });

      navigate('/my-reports');
    } catch (err) {
      if (err instanceof ApiError) {
        setServerError(err.message);
      } else {
        setServerError('Unable to submit lost report right now.');
      }
    }
  };

  return (
    <section aria-labelledby="report-lost-heading">
      <h1 id="report-lost-heading">Report a lost item</h1>
      {serverError ? (
        <p className="alert-error" role="alert">
          {serverError}
        </p>
      ) : null}

      <form className="form-card" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="field-group">
          <label className="field-label" htmlFor="lost-title">
            Item title (public)
          </label>
          <input
            id="lost-title"
            type="text"
            className="field-input"
            aria-invalid={Boolean(errors.title)}
            {...register('title')}
          />
          {errors.title ? (
            <p className="field-error" role="alert">
              {errors.title.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="lost-category">
            Category (public)
          </label>
          <select id="lost-category" className="field-select" {...register('category')}>
            {ITEM_CATEGORIES.map((item) => (
              <option key={item} value={item}>
                {CATEGORY_LABELS[item]}
              </option>
            ))}
          </select>
          {errors.category ? (
            <p className="field-error" role="alert">
              {errors.category.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="lost-area">
            Campus area (public)
          </label>
          <select id="lost-area" className="field-select" {...register('campusArea')}>
            {CAMPUS_AREAS.map((area) => (
              <option key={area} value={area}>
                {area}
              </option>
            ))}
          </select>
          {errors.campusArea ? (
            <p className="field-error" role="alert">
              {errors.campusArea.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="lost-date">
            Date lost (public)
          </label>
          <input
            id="lost-date"
            type="date"
            className="field-input"
            aria-invalid={Boolean(errors.dateLost)}
            {...register('dateLost')}
          />
          {errors.dateLost ? (
            <p className="field-error" role="alert">
              {errors.dateLost.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="lost-public-description">
            Public description
          </label>
          <textarea
            id="lost-public-description"
            rows={3}
            className="field-textarea"
            aria-invalid={Boolean(errors.publicDescription)}
            {...register('publicDescription')}
          />
          <p className="field-hint">
            Do not include phone numbers, emails, or serial numbers here.
          </p>
          {errors.publicDescription ? (
            <p className="field-error" role="alert">
              {errors.publicDescription.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="lost-photo">
            Optional photo (JPEG, PNG, or WebP, max 5 MB)
          </label>
          <input
            id="lost-photo"
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
            <img className={styles.preview} src={previewUrl} alt="Selected lost item preview" />
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="lost-private-details">
            Private identifying details (encrypted, never shown on board)
          </label>
          <textarea
            id="lost-private-details"
            rows={3}
            className="field-textarea"
            aria-invalid={Boolean(errors.privateDetails)}
            {...register('privateDetails')}
          />
          {errors.privateDetails ? (
            <p className="field-error" role="alert">
              {errors.privateDetails.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="lost-contact-phone">
            Contact phone (encrypted, private)
          </label>
          <input
            id="lost-contact-phone"
            type="tel"
            className="field-input"
            aria-invalid={Boolean(errors.contactPhone)}
            {...register('contactPhone')}
          />
          {errors.contactPhone ? (
            <p className="field-error" role="alert">
              {errors.contactPhone.message}
            </p>
          ) : null}
        </div>

        <div className="field-group">
          <label className="field-label" htmlFor="lost-contact-email">
            Contact email (encrypted, private)
          </label>
          <input
            id="lost-contact-email"
            type="email"
            className="field-input"
            aria-invalid={Boolean(errors.contactEmail)}
            {...register('contactEmail')}
          />
          {errors.contactEmail ? (
            <p className="field-error" role="alert">
              {errors.contactEmail.message}
            </p>
          ) : null}
        </div>

        <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Submitting...' : 'Submit lost report'}
        </button>
      </form>
    </section>
  );
}
