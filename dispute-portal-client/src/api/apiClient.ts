import axios from "axios";
import type {
  CreateDisputeRequest,
  Dispute,
  LoginRequest,
  LoginResponse,
  Transaction,
  UpdateDisputeStatusRequest,
} from "../types/types";

const API_BASE_URL = "http://localhost:7000";

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
});
//test
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

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

export async function getAdminDisputes(): Promise<Dispute[]> {
  const response = await apiClient.get<Dispute[]>("/api/admin/disputes");
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