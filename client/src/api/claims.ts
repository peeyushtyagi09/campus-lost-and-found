import type {
  ClaimDto,
  ClaimStatus,
  CompleteHandoverRequest,
  CreateClaimRequest,
  HandoverDto,
  StaffClaimReviewDto,
} from '../types/claims';
import { apiFetch } from './http';

export function submitClaim(request: CreateClaimRequest): Promise<ClaimDto> {
  return apiFetch<ClaimDto>('/api/v1/claims', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function getMyClaims(): Promise<ClaimDto[]> {
  return apiFetch<ClaimDto[]>('/api/v1/claims/mine');
}

export function withdrawClaim(claimId: string): Promise<ClaimDto> {
  return apiFetch<ClaimDto>(`/api/v1/claims/${encodeURIComponent(claimId)}/withdraw`, {
    method: 'POST',
  });
}

export function getReviewQueue(status?: ClaimStatus): Promise<StaffClaimReviewDto[]> {
  const qs = status ? `?status=${encodeURIComponent(status)}` : '';
  return apiFetch<StaffClaimReviewDto[]>(`/api/v1/claims/review${qs}`);
}

export function approveClaim(claimId: string, reviewNotes: string): Promise<StaffClaimReviewDto> {
  return apiFetch<StaffClaimReviewDto>(`/api/v1/claims/${encodeURIComponent(claimId)}/approve`, {
    method: 'POST',
    body: JSON.stringify({ reviewNotes }),
  });
}

export function rejectClaim(claimId: string, reviewNotes: string): Promise<StaffClaimReviewDto> {
  return apiFetch<StaffClaimReviewDto>(`/api/v1/claims/${encodeURIComponent(claimId)}/reject`, {
    method: 'POST',
    body: JSON.stringify({ reviewNotes }),
  });
}

export function completeHandover(
  claimId: string,
  request: CompleteHandoverRequest
): Promise<HandoverDto> {
  return apiFetch<HandoverDto>(`/api/v1/handovers/${encodeURIComponent(claimId)}/complete`, {
    method: 'POST',
    body: JSON.stringify(request),
  });
}
