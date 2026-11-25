import type { PagedResponse } from '../types/common';
import type {
  CreateLostReportRequest,
  OwnerReport,
  UpdateLostReportRequest,
} from '../types/reports';
import { apiFetch } from './http';

export function createLostReport(request: CreateLostReportRequest): Promise<OwnerReport> {
  return apiFetch<OwnerReport>('/api/v1/lost-reports', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function getMyLostReports(page = 1, pageSize = 20): Promise<PagedResponse<OwnerReport>> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<OwnerReport>>(`/api/v1/lost-reports/mine?${params.toString()}`);
}

export function getLostReportById(id: string): Promise<OwnerReport> {
  return apiFetch<OwnerReport>(`/api/v1/lost-reports/${encodeURIComponent(id)}`);
}

export function updateLostReport(
  id: string,
  request: UpdateLostReportRequest
): Promise<OwnerReport> {
  return apiFetch<OwnerReport>(`/api/v1/lost-reports/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  });
}

export function closeLostReport(id: string): Promise<void> {
  return apiFetch<void>(`/api/v1/lost-reports/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}
