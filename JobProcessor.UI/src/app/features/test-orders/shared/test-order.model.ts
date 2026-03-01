export interface TestOrderListItem {
  testOrderId: string;
  sampleId: string;
  testOrderType: string;
  status: string;
  retryCount: number;
  maxRetries: number;
  createdAt: string;
  updatedAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
