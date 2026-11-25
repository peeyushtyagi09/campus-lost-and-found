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
