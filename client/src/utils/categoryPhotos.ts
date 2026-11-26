import { resolveApiUrl } from '../api/http';
import bagPhoto from '../assets/categories/bag.svg';
import booksPhoto from '../assets/categories/books.svg';
import clothingPhoto from '../assets/categories/clothing.svg';
import idCardPhoto from '../assets/categories/id-card.svg';
import keysPhoto from '../assets/categories/keys.svg';
import laptopPhoto from '../assets/categories/laptop.svg';
import otherPhoto from '../assets/categories/other.svg';
import phonePhoto from '../assets/categories/phone.svg';
import walletPhoto from '../assets/categories/wallet.svg';
import { CATEGORY_LABELS, type ItemCategory } from '../types/common';

const CATEGORY_PHOTOS: Record<ItemCategory, string> = {
  Phone: phonePhoto,
  Laptop: laptopPhoto,
  Wallet: walletPhoto,
  IdCard: idCardPhoto,
  Keys: keysPhoto,
  Bag: bagPhoto,
  Clothing: clothingPhoto,
  Books: booksPhoto,
  Other: otherPhoto,
};

export interface ResolvedPhoto {
  src: string;
  alt: string;
  isFallback: boolean;
}

export function resolveReportPhoto(
  category: ItemCategory,
  title: string,
  photoUrl: string | null,
  imageFailed = false
): ResolvedPhoto {
  const categoryLabel = CATEGORY_LABELS[category] ?? category;
  if (photoUrl && !imageFailed) {
    return {
      src: resolveApiUrl(photoUrl),
      alt: `Sanitized photo of ${title}`,
      isFallback: false,
    };
  }

  return {
    src: CATEGORY_PHOTOS[category] ?? otherPhoto,
    alt: `${categoryLabel} preview image for ${title}`,
    isFallback: true,
  };
}
