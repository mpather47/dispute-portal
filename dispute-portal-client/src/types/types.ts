export type UserRole = "Customer" | "Admin";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  userId: number;
  fullName: string;
  email: string;
  role: UserRole;
}

export interface Transaction {
  id: number;
  reference: string;
  merchantName: string;
  amount: number;
  type: string;
  transactionDate: string;
  description: string;
  isDisputable: boolean;
  hasDispute: boolean;
}

export interface CreateDisputeRequest {
  transactionId: number;
  reason: string;
  customerNotes: string;
}

export interface DisputeEvent {
  status: string;
  message: string;
  createdBy: string;
  createdAt: string;
}

export interface Dispute {
  id: number;
  caseNumber: string;
  transactionId: number;
  merchantName: string;
  amount: number;
  reason: string;
  customerNotes: string;
  adminNotes?: string | null;
  status: string;
  createdAt: string;
  resolvedAt?: string | null;
  events: DisputeEvent[];
}

export interface UpdateDisputeStatusRequest {
  status: number;
  adminNotes?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface NotificationLog {
  id: number;
  subject: string;
  message: string;
  sentAt: string;
}