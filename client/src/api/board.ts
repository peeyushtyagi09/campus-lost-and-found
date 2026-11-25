import type { ItemCategory, PagedResponse } from '../types/common';
import type { PublicReport } from '../types/reports';
import { apiFetch } from './http';

export interface BoardQuery {
  category?: ItemCategory | '';
  area?: string;
  q?: string;
  page?: number;
  pageSize?: number;
}

export function getPublicBoard(query: BoardQuery): Promise<PagedResponse<PublicReport>> {
  const params = new URLSearchParams();
  if (query.category) {
    params.set('category', query.category);
  }
  if (query.area?.trim()) {
    params.set('area', query.area.trim());
  }
  if (query.q?.trim()) {
    params.set('q', query.q.trim());
  }
  params.set('page', String(query.page ?? 1));
  params.set('pageSize', String(query.pageSize ?? 12));

  return apiFetch<PagedResponse<PublicReport>>(`/api/v1/public/board?${params.toString()}`, {
    skipAuthRedirect: true,
  });
}
