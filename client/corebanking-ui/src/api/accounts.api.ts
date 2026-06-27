import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from './axios';
import type { SavingsAccountDto, SavingsTransactionDto, SubmitAccountRequest } from './types';

const BASE = '/api/v1/savingsaccounts';

export const accountKeys = {
  all: ['accounts'] as const,
  detail: (id: string) => ['accounts', id] as const,
  transactions: (id: string) => ['accounts', id, 'transactions'] as const,
};

export function useAccounts() {
  return useQuery<SavingsAccountDto[]>({
    queryKey: accountKeys.all,
    queryFn: () => api.get<SavingsAccountDto[]>(BASE).then((r) => r.data),
  });
}

export function useAccount(id: string) {
  return useQuery<SavingsAccountDto>({
    queryKey: accountKeys.detail(id),
    queryFn: () => api.get<SavingsAccountDto>(`${BASE}/${id}`).then((r) => r.data),
    enabled: !!id,
  });
}

export function useAccountTransactions(id: string) {
  return useQuery<SavingsTransactionDto[]>({
    queryKey: accountKeys.transactions(id),
    queryFn: () =>
      api.get<SavingsTransactionDto[]>(`${BASE}/${id}/transactions`).then((r) => r.data),
    enabled: !!id,
  });
}

export function useSubmitAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: SubmitAccountRequest) =>
      api.post<{ id: string }>(BASE, body).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: accountKeys.all }),
  });
}

function useAccountAction(action: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: Record<string, unknown> }) =>
      api.post(`${BASE}/${id}/${action}`, body),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: accountKeys.all });
      qc.invalidateQueries({ queryKey: accountKeys.detail(id) });
    },
  });
}

export const useApproveAccount = () => useAccountAction('approve');
export const useActivateAccount = () => useAccountAction('activate');
export const useRejectAccount = () => useAccountAction('reject');
export const useWithdrawAccount = () => useAccountAction('withdraw');
export const useCloseAccount = () => useAccountAction('close');
export const usePostInterest = () => useAccountAction('postinterest');

export function useDeposit() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      transactionDate,
      amount,
    }: {
      id: string;
      transactionDate: string;
      amount: number;
    }) => api.post(`${BASE}/${id}/transactions/deposit`, { transactionDate, amount }),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: accountKeys.detail(id) });
      qc.invalidateQueries({ queryKey: accountKeys.transactions(id) });
    },
  });
}

export function useWithdrawMoney() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      transactionDate,
      amount,
    }: {
      id: string;
      transactionDate: string;
      amount: number;
    }) => api.post(`${BASE}/${id}/transactions/withdraw`, { transactionDate, amount }),
    onSuccess: (_data, { id }) => {
      qc.invalidateQueries({ queryKey: accountKeys.detail(id) });
      qc.invalidateQueries({ queryKey: accountKeys.transactions(id) });
    },
  });
}
