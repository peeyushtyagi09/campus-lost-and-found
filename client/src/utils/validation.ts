export const MAX_PHOTO_BYTES = 5 * 1024 * 1024;
export const ALLOWED_PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp'] as const;

const EMAIL_PATTERN = /[^\s@]+@[^\s@]+\.[^\s@]+/;
const PHONE_CANDIDATE_PATTERN = /(?<!\d)(?:\+?\d[\d\s().-]{8,}\d)(?!\d)/g;

export function containsContactInfo(value: string): boolean {
  if (!value.trim()) {
    return false;
  }

  if (EMAIL_PATTERN.test(value)) {
    return true;
  }

  const matches = value.match(PHONE_CANDIDATE_PATTERN);
  if (!matches) {
    return false;
  }

  return matches.some((candidate) => candidate.replace(/\D/g, '').length >= 10);
}

export function validatePhotoFile(file: File): string | null {
  const allowed = ALLOWED_PHOTO_TYPES.includes(file.type as (typeof ALLOWED_PHOTO_TYPES)[number]);
  if (!allowed) {
    return 'Photo must be a JPEG, PNG, or WebP image.';
  }

  if (file.size === 0) {
    return 'Selected photo file is empty.';
  }

  if (file.size > MAX_PHOTO_BYTES) {
    return 'Photo must be 5 MB or smaller.';
  }

  return null;
}
