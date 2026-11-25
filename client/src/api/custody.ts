import type { CustodyTimelineDto } from '../types/claims';
import { apiFetch } from './http';

export function getCustodyTimeline(reportId: string): Promise<CustodyTimelineDto> {
  return apiFetch<CustodyTimelineDto>(`/api/v1/custody/${encodeURIComponent(reportId)}`);
}
