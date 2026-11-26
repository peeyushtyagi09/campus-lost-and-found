import type { PagedResponse } from '../types/common';
import type { CreateFoundReportRequest, FoundOwnerReport } from '../types/reports';
import { apiFetch } from './http';

export function createFoundReport(request: CreateFoundReportRequest): Promise<FoundOwnerReport> {
  return apiFetch<FoundOwnerReport>('/api/v1/found-reports', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function getMyFoundReports(
  page = 1,
  pageSize = 20
): Promise<PagedResponse<FoundOwnerReport>> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<FoundOwnerReport>>(
    `/api/v1/found-reports/mine?${params.toString()}`
  );
}

export function closeFoundReport(id: string): Promise<void> {
  return apiFetch<void>(`/api/v1/found-reports/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  });
}
