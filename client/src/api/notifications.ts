import type { NotificationDto } from '../types/admin';
import { apiFetch } from './http';

export function getNotifications(): Promise<NotificationDto[]> {
  return apiFetch<NotificationDto[]>('/api/v1/notifications');
}

export function markNotificationRead(id: string): Promise<NotificationDto> {
  return apiFetch<NotificationDto>(`/api/v1/notifications/${encodeURIComponent(id)}/read`, {
    method: 'POST',
  });
}
