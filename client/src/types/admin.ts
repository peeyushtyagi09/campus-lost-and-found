import type { UserRole } from './auth';

export type NotificationType = 'MatchFound' | 'ClaimStatusChanged' | 'HandoverCompleted';

export interface NotificationDto {
  id: string;
  type: NotificationType;
  message: string;
  relatedEntityId: string | null;
  isRead: boolean;
  createdAt: string;
}

export type DisposalMethod = 'Donate' | 'Recycle' | 'Destroy';

export interface DisposalDto {
  id: string;
  foundReportId: string;
  method: DisposalMethod;
  disposedAt: string;
}

export interface AdminUserDto {
  id: string;
  displayName: string;
  role: UserRole;
  createdAt: string;
}

export interface CreateStaffRequest {
  displayName: string;
  email: string;
  password: string;
}

export interface AuditEventDto {
  id: string;
  action: string;
  actorRole: UserRole;
  targetId: string | null;
  summary: string;
  timestamp: string;
}
