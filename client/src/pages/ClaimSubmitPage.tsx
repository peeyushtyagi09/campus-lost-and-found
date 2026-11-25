import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { z } from 'zod';
import { submitClaim } from '../api/claims';
import { ApiError } from '../api/http';

const claimSchema = z.object({
  verificationAnswers: z
    .string()
    .trim()
    .min(5, 'Provide at least 5 characters describing unique identifying marks.')
    .max(1500),
});

type ClaimFormValues = z.infer<typeof claimSchema>;

export function ClaimSubmitPage() {
  const { foundReportId = '' } = useParams();
  const [searchParams] = useSearchParams();
  const lostReportId = searchParams.get('lostReportId');
  const navigate = useNavigate();
  const [serverError, setServerError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ClaimFormValues>({
    resolver: zodResolver(claimSchema),
  });

  const onSubmit = async (values: ClaimFormValues) => {
    setServerError(null);
    try {
      await submitClaim({
        foundReportId,
        lostReportId,
        verificationAnswers: values.verificationAnswers,
      });
      navigate('/my-claims');
    } catch (err) {
      setServerError(err instanceof ApiError ? err.message : 'Unable to submit claim.');
    }
  };

  return (
    <section aria-labelledby="claim-heading">
      <h1 id="claim-heading">Submit ownership claim</h1>
      <p className="field-hint">
        Your proof answers are encrypted and visible only to campus staff reviewing this claim.
      </p>
      {serverError ? (
        <p className="alert-error" role="alert">
          {serverError}
        </p>
      ) : null}
      <form className="form-card" onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="field-group">
          <label className="field-label" htmlFor="claim-answers">
            Proof of ownership (serial number, unique marks, contents)
          </label>
          <textarea
            id="claim-answers"
            rows={4}
            className="field-textarea"
            aria-invalid={Boolean(errors.verificationAnswers)}
            {...register('verificationAnswers')}
          />
          {errors.verificationAnswers ? (
            <p className="field-error" role="alert">
              {errors.verificationAnswers.message}
            </p>
          ) : null}
        </div>
        <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
          {isSubmitting ? 'Submitting...' : 'Submit claim'}
        </button>
      </form>
    </section>
  );
}
