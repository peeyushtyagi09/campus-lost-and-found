export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export type ItemCategory =
  'Phone' | 'Laptop' | 'Wallet' | 'IdCard' | 'Keys' | 'Bag' | 'Clothing' | 'Books' | 'Other';

export const ITEM_CATEGORIES: readonly ItemCategory[] = [
  'Phone',
  'Laptop',
  'Wallet',
  'IdCard',
  'Keys',
  'Bag',
  'Clothing',
  'Books',
  'Other',
];

export const CATEGORY_LABELS: Record<ItemCategory, string> = {
  Phone: 'Phone',
  Laptop: 'Laptop',
  Wallet: 'Wallet',
  IdCard: 'ID Card',
  Keys: 'Keys',
  Bag: 'Bag',
  Clothing: 'Clothing',
  Books: 'Books',
  Other: 'Other',
};

export type ReportStatus = 'Open' | 'Matched' | 'Claimed' | 'Returned' | 'Closed';

export const CAMPUS_AREAS: readonly string[] = [
  'Library',
  'Student Union',
  'Hostel Block A',
  'Engineering Block',
  'Main Ground',
  'Cafeteria',
  'Science Building',
  'Sports Complex',
];
