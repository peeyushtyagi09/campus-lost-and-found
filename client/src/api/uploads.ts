import type { UploadPhotoResponse } from '../types/reports';
import { apiFetch } from './http';

export function uploadPhoto(file: File): Promise<UploadPhotoResponse> {
  const formData = new FormData();
  formData.append('file', file);
  return apiFetch<UploadPhotoResponse>('/api/v1/uploads/photos', {
    method: 'POST',
    body: formData,
  });
}
