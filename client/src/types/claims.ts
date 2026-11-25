import type { UserRole } from './auth';
import type { PublicReport } from './reports';

export type ClaimStatus = 'Pending' | 'Approved' | 'Rejected' | 'Withdrawn';

export interface CreateClaimRequest {
  foundReportId: string;
  lostReportId?: string | null;
  verificationAnswers: string;
}

export interface ClaimDto {
  id: string;
  foundReportId: string;
  lostReportId: string | null;
  status: ClaimStatus;
  verificationAnswers: string;
  reviewNotes: string | null;
  createdAt: string;
  foundItem: PublicReport | null;
}

export interface StaffClaimReviewDto {
  id: string;
  foundReportId: string;
  lostReportId: string | null;
  status: ClaimStatus;
  verificationAnswers: string;
  foundVerificationDetails: string;
  foundStorageNotes: string;
  reviewNotes: string | null;
  createdAt: string;
  foundItem: PublicReport | null;
}

export interface CompleteHandoverRequest {
  witnessConfirmed: boolean;
  idChecked: boolean;
  notes?: string;
}

export interface HandoverDto {
  id: string;
  claimId: string;
  foundReportId: string;
  witnessConfirmed: boolean;
  idChecked: boolean;
  completedAt: string;
}

export type CustodyEventType =
  | 'Received'
  | 'Stored'
  | 'ClaimSubmitted'
  | 'ClaimApproved'
  | 'HandedOver'
  | 'Rejected'
  | 'Disposed';

export interface CustodyEventDto {
  id: string;
  reportId: string;
  sequenceNumber: number;
  eventType: CustodyEventType;
  actorRole: UserRole;
  timestamp: string;
  notes: string;
  previousEventHash: string;
  eventHash: string;
}

export interface CustodyTimelineDto {
  reportId: string;
  isChainValid: boolean;
  events: CustodyEventDto[];
}
