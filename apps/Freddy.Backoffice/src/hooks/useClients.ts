import {
  useQuery,
  useMutation,
  useQueryClient,
} from '@tanstack/react-query';
import {
  getClients,
  getClient,
  createClient,
  updateClient,
  deleteClient,
} from '../lib/adminApi';
import type { CreateClientRequest, UpdateClientRequest } from '../types/admin';

const CLIENTS_KEY = ['clients'] as const;
const clientKey = (id: string) => ['clients', id] as const;

export function useClients(params?: { isActive?: boolean; search?: string }) {
  return useQuery({
    queryKey: [...CLIENTS_KEY, params],
    queryFn: () => getClients(params),
  });
}

export function useClient(id: string | undefined) {
  return useQuery({
    queryKey: clientKey(id!),
    queryFn: () => getClient(id!),
    enabled: !!id,
  });
}

export function useCreateClient() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateClientRequest) => createClient(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CLIENTS_KEY });
    },
  });
}

export function useUpdateClient(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateClientRequest) => updateClient(id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CLIENTS_KEY });
      void queryClient.invalidateQueries({ queryKey: clientKey(id) });
    },
  });
}

export function useDeleteClient() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteClient(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: CLIENTS_KEY });
    },
  });
}
