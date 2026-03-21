export interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  data?: T;
  errors?: string[];
  timestamp: string;
}

export interface PaginationMeta {
  currentPage: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface PagedResponse<T> extends ApiResponse<T[]> {
  pagination?: PaginationMeta;
}
