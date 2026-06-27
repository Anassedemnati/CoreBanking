import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from './axios';
import type { ClientDto, RegisterClientRequest } from './types';

const BASE = '/api/v1/clients';

export const clientKeys = {
  all: ['clients'] as const,
  detail: (id: string) => ['clients', id] as const,
};

export function useClients() {
  return useQuery<ClientDto[]>({
    queryKey: clientKeys.all,
    queryFn: () => api.get<ClientDto[]>(BASE).then((r) => r.data),
  });
}

export function useClient(id: string) {
  return useQuery<ClientDto>({
    queryKey: clientKeys.detail(id),
    queryFn: () => api.get<ClientDto>(`${BASE}/${id}`).then((r) => r.data),
    enabled: !!id,
  });
}

export function useRegisterClient() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: RegisterClientRequest) =>
      api.post<{ id: string }>(BASE, body).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: clientKeys.all }),
  });
}

export function useActivateClient() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, activationDate }: { id: string; activationDate: string }) =>
      api.post(`${BASE}/${id}/activate`, { activationDate }),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: clientKeys.all });
      qc.invalidateQueries({ queryKey: clientKeys.detail(id) });
    },
  });
}
