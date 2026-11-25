import type { DisposalDto, DisposalMethod } from '../types/admin';
import type { PublicReport } from '../types/reports';
import { apiFetch } from './http';

export function getEligibleDisposals(): Promise<PublicReport[]> {
  return apiFetch<PublicReport[]>('/api/v1/disposals/eligible');
}

export function approveDisposal(
  foundReportId: string,
  method: DisposalMethod,
  notes: string
): Promise<DisposalDto> {
  return apiFetch<DisposalDto>(`/api/v1/disposals/${encodeURIComponent(foundReportId)}/approve`, {
    method: 'POST',
    body: JSON.stringify({ method, notes }),
  });
}
