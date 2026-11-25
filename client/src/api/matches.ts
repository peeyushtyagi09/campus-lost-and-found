import type { ItemMatch } from '../types/reports';
import { apiFetch } from './http';

export function getMyMatches(): Promise<ItemMatch[]> {
  return apiFetch<ItemMatch[]>('/api/v1/matches/mine');
}

export function runMatching(): Promise<{ matchesEvaluated: number }> {
  return apiFetch<{ matchesEvaluated: number }>('/api/v1/matches/run', {
    method: 'POST',
  });
}

export function confirmMatch(matchId: string): Promise<ItemMatch> {
  return apiFetch<ItemMatch>(`/api/v1/matches/${encodeURIComponent(matchId)}/confirm`, {
    method: 'POST',
  });
}

export function dismissMatch(matchId: string): Promise<ItemMatch> {
  return apiFetch<ItemMatch>(`/api/v1/matches/${encodeURIComponent(matchId)}/dismiss`, {
    method: 'POST',
  });
}
