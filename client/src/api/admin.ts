import type { AdminUserDto, AuditEventDto, CreateStaffRequest } from '../types/admin';
import type { UserRole } from '../types/auth';
import type { PagedResponse } from '../types/common';
import { apiFetch } from './http';

export function getAdminUsers(): Promise<AdminUserDto[]> {
  return apiFetch<AdminUserDto[]>('/api/v1/admin/users');
}

export function createStaffAccount(request: CreateStaffRequest): Promise<AdminUserDto> {
  return apiFetch<AdminUserDto>('/api/v1/admin/staff', {
    method: 'POST',
    body: JSON.stringify(request),
  });
}

export function updateUserRole(userId: string, role: UserRole): Promise<AdminUserDto> {
  return apiFetch<AdminUserDto>(`/api/v1/admin/users/${encodeURIComponent(userId)}/role`, {
    method: 'PATCH',
    body: JSON.stringify({ role }),
  });
}

export function getAuditLogs(page = 1, pageSize = 25): Promise<PagedResponse<AuditEventDto>> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<AuditEventDto>>(`/api/v1/admin/audit-logs?${params.toString()}`);
}
