import type { ItemCategory, ReportStatus } from './common';

export interface PublicReport {
  id: string;
  title: string;
  category: ItemCategory;
  publicDescription: string;
  campusArea: string;
  dateLost: string;
  photoUrl: string | null;
  status: ReportStatus;
  postedAt: string;
  heldAt?: string | null;
}

export interface FoundOwnerReport {
  id: string;
  title: string;
  category: ItemCategory;
  publicDescription: string;
  campusArea: string;
  dateFound: string;
  heldAt: string;
  photoFileName: string | null;
  photoUrl: string | null;
  status: ReportStatus;
  storageNotes: string;
  finderContact: string;
  verificationDetails: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateFoundReportRequest {
  title: string;
  category: ItemCategory;
  publicDescription: string;
  campusArea: string;
  dateFound: string;
  heldAt: string;
  photoFileName: string | null;
  storageNotes: string;
  finderContact: string;
  verificationDetails: string;
}

export type MatchStatus = 'Suggested' | 'Confirmed' | 'Dismissed';

export interface ItemMatch {
  id: string;
  lostReportId: string;
  foundReportId: string;
  score: number;
  explanation: string;
  status: MatchStatus;
  createdAt: string;
  foundItem: PublicReport;
}

export interface OwnerReport {
  id: string;
  title: string;
  category: ItemCategory;
  publicDescription: string;
  campusArea: string;
  dateLost: string;
  photoFileName: string | null;
  photoUrl: string | null;
  status: ReportStatus;
  privateDetails: string;
  contactPhone: string;
  contactEmail: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLostReportRequest {
  title: string;
  category: ItemCategory;
  publicDescription: string;
  campusArea: string;
  dateLost: string;
  photoFileName: string | null;
  privateDetails: string;
  contactPhone: string;
  contactEmail: string;
}

export type UpdateLostReportRequest = CreateLostReportRequest;

export interface UploadPhotoResponse {
  fileName: string;
  url: string;
}
