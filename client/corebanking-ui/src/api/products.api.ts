import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from './axios';
import type { SavingsProductDto, CreateSavingsProductRequest } from './types';

const BASE = '/api/v1/savingsproducts';

export const productKeys = {
  all: ['products'] as const,
  detail: (id: string) => ['products', id] as const,
};

export function useProducts() {
  return useQuery<SavingsProductDto[]>({
    queryKey: productKeys.all,
    queryFn: () => api.get<SavingsProductDto[]>(BASE).then((r) => r.data),
  });
}

export function useProduct(id: string) {
  return useQuery<SavingsProductDto>({
    queryKey: productKeys.detail(id),
    queryFn: () => api.get<SavingsProductDto>(`${BASE}/${id}`).then((r) => r.data),
    enabled: !!id,
  });
}

export function useCreateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateSavingsProductRequest) =>
      api.post<{ id: string }>(BASE, body).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: productKeys.all }),
  });
}
