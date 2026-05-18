import axios from "axios";
import type {
  CreateDisputeRequest,
  Dispute,
  LoginRequest,
  LoginResponse,
  NotificationLog,
  PagedResult,
  Transaction,
  UpdateDisputeStatusRequest,
} from "../types/types";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && window.location.pathname !== "/login") {
      ["token", "userId", "fullName", "email", "role"].forEach((key) =>
        localStorage.removeItem(key)
      );
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>("/api/auth/login", request);
  return response.data;
}

export async function getTransactions(): Promise<Transaction[]> {
  const response = await apiClient.get<Transaction[]>("/api/transactions");
  return response.data;
}

export async function createDispute(
  request: CreateDisputeRequest
): Promise<Dispute> {
  const response = await apiClient.post<Dispute>("/api/disputes", request);
  return response.data;
}

export async function getMyDisputes(): Promise<Dispute[]> {
  const response = await apiClient.get<Dispute[]>("/api/disputes/my");
  return response.data;
}

export async function getDisputeById(id: number): Promise<Dispute> {
  const response = await apiClient.get<Dispute>(`/api/disputes/${id}`);
  return response.data;
}

export async function getAdminDisputes(
  page = 1,
  pageSize = 10,
  status?: number
): Promise<PagedResult<Dispute>> {
  const params: Record<string, string | number> = { page, pageSize };
  if (status !== undefined) params.status = status;
  const response = await apiClient.get<PagedResult<Dispute>>("/api/admin/disputes", { params });
  return response.data;
}

export async function updateDisputeStatus(
  id: number,
  request: UpdateDisputeStatusRequest
): Promise<Dispute> {
  const response = await apiClient.put<Dispute>(
    `/api/admin/disputes/${id}/status`,
    request
  );

  return response.data;
}

export async function getNotifications(): Promise<NotificationLog[]> {
  const response = await apiClient.get<NotificationLog[]>("/api/notifications");
  return response.data;
}